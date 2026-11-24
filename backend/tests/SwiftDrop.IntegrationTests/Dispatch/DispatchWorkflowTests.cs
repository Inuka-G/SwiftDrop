using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.SharedKernel.Messaging;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.IntegrationTests.Auth;

namespace SwiftDrop.IntegrationTests.Dispatch;

[Collection(ApiCollection.Name)]
public sealed class DispatchWorkflowTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Admin_can_list_created_parcels_and_unassigned_board_rows()
    {
        var parcelId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var consumer = scope.ServiceProvider.GetRequiredService<ParcelCreatedConsumer>();
            await consumer.HandleAsync(new ParcelCreatedMessage(Guid.NewGuid(), parcelId,
                "SD-ADMIN-LIST-1", Guid.NewGuid(), createdAt), default);
        }

        var token = await client.LoginAsync("admin");
        using var allResponse = await client.GetWithTokenAsync("/api/v1/dispatch/parcels?trackingNumber=SD-ADMIN-LIST-1", token);
        allResponse.EnsureSuccessStatusCode();
        var allParcels = await allResponse.Content.ReadFromJsonAsync<DispatchParcelPage>();
        var parcel = Assert.Single(allParcels!.Items);
        Assert.Equal(parcelId, parcel.ParcelId);
        Assert.Equal("Created", parcel.Status);
        Assert.Equal("UNASSIGNED", parcel.AssignmentState);

        using var unassignedResponse = await client.GetWithTokenAsync("/api/v1/dispatch/parcels/unassigned", token);
        unassignedResponse.EnsureSuccessStatusCode();
        var unassigned = await unassignedResponse.Content.ReadFromJsonAsync<UnassignedParcelPage>();
        Assert.Contains(unassigned!.Items, x => x.ParcelId == parcelId);
    }

    [Fact]
    public async Task Customer_booking_event_is_projected_to_admin_unassigned_board()
    {
        var customerToken = await client.LoginAsync("customer1");
        using var zonesResponse = await client.GetAsync("/api/v1/zones");
        zonesResponse.EnsureSuccessStatusCode();
        var zones = await zonesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var zoneId = zones.EnumerateArray().First().GetProperty("id").GetGuid();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/parcels")
        {
            Content = JsonContent.Create(new
            {
                zoneId,
                pickupAddress = "1 Main St, Colombo",
                pickupContactName = "Sender",
                pickupContactPhone = "+94771234567",
                dropoffAddress = "2 Hill St, Kandy",
                recipientName = "Recipient",
                recipientPhone = "+94779876543",
                weightKg = 2m,
                sizeCategory = "Small",
                paymentMethod = "COD",
                serviceType = 1
            })
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", customerToken);
        using var bookingResponse = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, bookingResponse.StatusCode);
        var booked = await bookingResponse.Content.ReadFromJsonAsync<JsonElement>();
        var parcelId = booked.GetProperty("parcelId").GetGuid();
        var trackingNumber = booked.GetProperty("trackingNumber").GetString()!;

        // The test Host records the outbox publication instead of running RabbitMQ. Confirm the
        // event was emitted, then let the Dispatch board reconcile directly from the Parcels
        // contract as it does when a consumer queue missed the publication.
        (Guid EventId, string RoutingKey, string Payload) createdMessage = default;
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (DateTime.UtcNow < deadline && createdMessage == default)
        {
            foreach (var published in factory.Events.Published.Where(x => x.RoutingKey == ParcelEventRoutingKeys.Created))
            {
                using var payload = JsonDocument.Parse(published.Payload);
                if (payload.RootElement.TryGetProperty("parcelId", out var id) && id.GetGuid() == parcelId)
                {
                    createdMessage = published;
                    break;
                }
            }
            if (createdMessage == default) await Task.Delay(50);
        }
        Assert.NotEqual(default, createdMessage);

        var adminToken = await client.LoginAsync("admin");
        using var boardResponse = await client.GetWithTokenAsync(
            $"/api/v1/dispatch/parcels/unassigned?zone={zoneId}", adminToken);
        boardResponse.EnsureSuccessStatusCode();
        var board = await boardResponse.Content.ReadFromJsonAsync<UnassignedParcelPage>();
        var parcel = Assert.Single(board!.Items, item => item.ParcelId == parcelId);
        Assert.Equal(trackingNumber, parcel.TrackingNumber);
    }

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

        var assignmentResult = await assignResponse.Content.ReadFromJsonAsync<AssignmentDto>();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IIntegrationEventHandler<CourierAssignedIntegrationEvent>>()
                .HandleAsync(new CourierAssignedIntegrationEvent(Guid.NewGuid(), parcelId, "SD-WORK-1",
                    courierId, assignmentResult!.AssignedAt.UtcDateTime), default);
        }
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
