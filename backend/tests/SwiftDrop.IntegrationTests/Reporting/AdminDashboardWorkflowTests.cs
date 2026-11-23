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
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.IntegrationTests.Reporting;

[Collection(ApiCollection.Name)]
public sealed class AdminDashboardWorkflowTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Created_assignment_and_delivery_events_update_both_dashboard_groups_once()
    {
        var token = await client.LoginAsync("admin");
        var beforeReport = await GetJsonAsync("/api/v1/reports/summary", token);
        var courierId = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        // Use an isolated reporting date outside the demo seed window so the event delta is deterministic.
        var reportDate = DateOnly.FromDateTime(createdAt.UtcDateTime).AddDays(60);
        createdAt = new DateTimeOffset(reportDate.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)), DateTimeKind.Utc));
        var dailyBefore = await GetJsonAsync($"/api/v1/reports/deliveries-per-day?from={reportDate:yyyy-MM-dd}&to={reportDate:yyyy-MM-dd}", token);
        var createdBefore = dailyBefore[0].GetProperty("created").GetInt32();
        var deliveredBefore = dailyBefore[0].GetProperty("delivered").GetInt32();
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
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ParcelCreatedIntegrationEvent>>()
                .HandleAsync(new ParcelCreatedIntegrationEvent(eventId, parcelId, "DASHBOARD-TEST", null,
                    Guid.NewGuid(), createdAt.UtcDateTime), default);
        }

        var createdReport = await GetJsonAsync("/api/v1/reports/summary", token);
        var dailyAfterCreated = await GetJsonAsync($"/api/v1/reports/deliveries-per-day?from={reportDate:yyyy-MM-dd}&to={reportDate:yyyy-MM-dd}", token);
        Assert.Equal(createdBefore + 1, dailyAfterCreated[0].GetProperty("created").GetInt32());

        var assignment = await client.PostAsJsonWithTokenAsync($"/api/v1/dispatch/parcels/{parcelId}/assign",
            new { courierId }, token);
        Assert.Equal(HttpStatusCode.OK, assignment.StatusCode);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<CourierAssignedIntegrationEvent>>()
                .HandleAsync(new CourierAssignedIntegrationEvent(Guid.NewGuid(), parcelId, "DASHBOARD-TEST",
                    courierId, DateTime.UtcNow), default);
        }
        var terminalEventId = Guid.NewGuid();
        // The assignment endpoint delegates to Parcels; emulate its committed CourierAssigned event
        // before applying a later status event to Dispatch's projection.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<CourierAssignedIntegrationEvent>>()
                .HandleAsync(new CourierAssignedIntegrationEvent(Guid.NewGuid(), parcelId, "DASHBOARD-TEST",
                    courierId, DateTime.UtcNow), default);
            await scope.ServiceProvider.GetRequiredService<SwiftDrop.Dispatch.Business.Consumers.ParcelStatusChangedConsumer>()
                .HandleAsync(new ParcelStatusChangedMessage(terminalEventId, parcelId, "DELIVERED"), default);
            var reportingStatus = scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<ParcelStatusChangedIntegrationEvent>>();
            await reportingStatus.HandleAsync(new ParcelStatusChangedIntegrationEvent(terminalEventId, parcelId,
                "DASHBOARD-TEST", null, courierId, "Out for Delivery", "DELIVERED", null,
                createdAt.UtcDateTime), default);
            await scope.ServiceProvider.GetRequiredService<SwiftDrop.Dispatch.Business.Consumers.ParcelStatusChangedConsumer>()
                .HandleAsync(new ParcelStatusChangedMessage(terminalEventId, parcelId, "DELIVERED"), default);
            await reportingStatus.HandleAsync(new ParcelStatusChangedIntegrationEvent(terminalEventId, parcelId,
                "DASHBOARD-TEST", null, courierId, "Out for Delivery", "DELIVERED", null,
                createdAt.UtcDateTime), default);
        }

        var dailyFinal = await GetJsonAsync($"/api/v1/reports/deliveries-per-day?from={reportDate:yyyy-MM-dd}&to={reportDate:yyyy-MM-dd}", token);
        Assert.Equal(deliveredBefore + 1,
            dailyFinal[0].GetProperty("delivered").GetInt32());
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
