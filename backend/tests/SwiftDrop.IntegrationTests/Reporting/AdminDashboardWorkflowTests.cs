using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.IntegrationTests.Auth;
using SwiftDrop.Reporting.Business.Consumers;

namespace SwiftDrop.IntegrationTests.Reporting;

[Collection(ApiCollection.Name)]
public sealed class AdminDashboardWorkflowTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Created_assignment_and_delivery_events_update_both_dashboard_groups_once()
    {
        var token = await client.LoginAsync("admin");
        var beforeDispatch = await GetJsonAsync("/api/v1/dispatch/summary", token);
        var beforeReport = await GetJsonAsync("/api/v1/reports/summary", token);
        var courierId = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var eventId = Guid.NewGuid();
        await using (var setupScope = factory.Services.CreateAsyncScope())
        {
            var db = setupScope.ServiceProvider.GetRequiredService<DispatchDbContext>();
            db.CourierProfiles.Add(new CourierProfile { CourierId = courierId, IsAvailable = true });
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<SwiftDrop.Dispatch.Business.Consumers.ParcelCreatedConsumer>();
            await consumer.HandleAsync(new ParcelCreatedMessage(eventId, parcelId, "DASHBOARD-TEST", Guid.NewGuid(), createdAt), default);
            await scope.ServiceProvider.GetRequiredService<SwiftDrop.Reporting.Business.Consumers.ParcelCreatedConsumer>()
                .HandleAsync(new ParcelCreatedReceived(eventId, createdAt), default);
        }

        var afterCreatedDispatch = await GetJsonAsync("/api/v1/dispatch/summary", token);
        var createdReport = await GetJsonAsync("/api/v1/reports/summary", token);
        Assert.Equal(beforeDispatch.GetProperty("unassignedCount").GetInt32() + 1,
            afterCreatedDispatch.GetProperty("unassignedCount").GetInt32());
        Assert.Equal(beforeReport.GetProperty("created").GetInt32() + 1,
            createdReport.GetProperty("created").GetInt32());

        var assignment = await client.PostAsJsonWithTokenAsync($"/api/v1/dispatch/parcels/{parcelId}/assign",
            new { courierId }, token);
        Assert.Equal(HttpStatusCode.OK, assignment.StatusCode);
        var assignedDispatch = await GetJsonAsync("/api/v1/dispatch/summary", token);
        Assert.Equal(afterCreatedDispatch.GetProperty("activeDeliveries").GetInt32() + 1,
            assignedDispatch.GetProperty("activeDeliveries").GetInt32());

        var terminalEventId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SwiftDrop.Dispatch.Business.Consumers.ParcelStatusChangedConsumer>()
                .HandleAsync(new ParcelStatusChangedMessage(terminalEventId, parcelId, "DELIVERED"), default);
            var reportingStatus = scope.ServiceProvider.GetRequiredService<SwiftDrop.Reporting.Business.Consumers.ParcelStatusChangedConsumer>();
            await reportingStatus.HandleAsync(new ParcelStatusChangedReceived(terminalEventId, DateTimeOffset.UtcNow, "DELIVERED"), default);
            await scope.ServiceProvider.GetRequiredService<SwiftDrop.Dispatch.Business.Consumers.ParcelStatusChangedConsumer>()
                .HandleAsync(new ParcelStatusChangedMessage(terminalEventId, parcelId, "DELIVERED"), default);
            await reportingStatus.HandleAsync(new ParcelStatusChangedReceived(terminalEventId, DateTimeOffset.UtcNow, "DELIVERED"), default);
        }

        var finalDispatch = await GetJsonAsync("/api/v1/dispatch/summary", token);
        var finalReport = await GetJsonAsync("/api/v1/reports/summary", token);
        Assert.Equal(assignedDispatch.GetProperty("activeDeliveries").GetInt32() - 1,
            finalDispatch.GetProperty("activeDeliveries").GetInt32());
        Assert.Equal(createdReport.GetProperty("delivered").GetInt32() + 1,
            finalReport.GetProperty("delivered").GetInt32());
    }

    private async Task<JsonElement> GetJsonAsync(string endpoint, string token)
    {
        var response = await client.GetWithTokenAsync(endpoint, token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).Clone();
    }
}

internal static class DashboardHttpExtensions
{
    public static Task<HttpResponseMessage> PostAsJsonWithTokenAsync(this HttpClient client, string url, object body, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }
}
