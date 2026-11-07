using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;

namespace SwiftDrop.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public sealed class DispatchWorkflowTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Assignment_increases_workload_and_terminal_status_decreases_it()
    {
        var courierId = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        await SeedAsync(new CourierProfile { CourierId = courierId, IsAvailable = true },
            new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD-WORK-1", State = "UNASSIGNED" });
        var token = await client.LoginAsync("admin");

        var assignResponse = await PostWithTokenAsync($"/api/v1/dispatch/parcels/{parcelId}/assign", new { courierId }, token);
        Assert.Equal(HttpStatusCode.OK, assignResponse.StatusCode);
        Assert.Equal(1, await GetWorkloadAsync(courierId, token));

        using (var scope = factory.Services.CreateScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<ParcelStatusChangedConsumer>();
            await consumer.HandleAsync(new ParcelStatusChangedMessage(Guid.NewGuid(), parcelId, "DELIVERED"), default);
        }
        Assert.Equal(0, await GetWorkloadAsync(courierId, token));
    }

    [Fact]
    public async Task Assignment_to_unavailable_courier_is_rejected()
    {
        var courierId = Guid.NewGuid();
        var parcelId = Guid.NewGuid();
        await SeedAsync(new CourierProfile { CourierId = courierId, IsAvailable = false },
            new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD-WORK-2", State = "UNASSIGNED" });
        var token = await client.LoginAsync("admin");

        var response = await PostWithTokenAsync($"/api/v1/dispatch/parcels/{parcelId}/assign", new { courierId }, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<int> GetWorkloadAsync(Guid courierId, string token)
    {
        using var response = await client.GetWithTokenAsync("/api/v1/dispatch/couriers", token);
        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<CourierProfilePage>();
        return page!.Items.Single(x => x.CourierId == courierId).ActiveDeliveryCount;
    }

    private async Task<HttpResponseMessage> PostWithTokenAsync(string path, object body, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = JsonContent.Create(body) };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private async Task SeedAsync(CourierProfile profile, ParcelAssignment assignment)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DispatchDbContext>();
        await db.CourierProfiles.AddAsync(profile);
        await db.Assignments.AddAsync(assignment);
        await db.SaveChangesAsync();
    }
}
