using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.IntegrationTests.Auth;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;

namespace SwiftDrop.IntegrationTests.Parcels;

[Collection(ApiCollection.Name)]
public sealed class CourierDeliveryTests(SwiftDropApiFactory factory)
{
    private const string Deliveries = "/api/v1/courier/deliveries";
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var response = await client.GetAsync(Deliveries);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("customer1")]
    [InlineData("merchant1")]
    [InlineData("admin")]
    public async Task Only_couriers_can_use_courier_endpoints(string username)
    {
        var token = await client.LoginAsync(username);
        var response = await client.GetWithTokenAsync(Deliveries, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Courier_sees_assigned_delivery_in_todays_list()
    {
        var (token, courierId) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId);

        var response = await client.GetWithTokenAsync(Deliveries, token);

        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<JsonElement>();
        var item = items.EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == parcelId);
        Assert.Equal(ParcelStatuses.Created, item.GetProperty("currentStatus").GetString());
        Assert.Equal(ParcelStatuses.PickedUp, item.GetProperty("allowedNextStatuses")[0].GetString());
    }

    [Fact]
    public async Task Courier_cannot_access_another_couriers_delivery()
    {
        var (token, _) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId: Guid.NewGuid());

        var view = await client.GetWithTokenAsync($"{Deliveries}/{parcelId}", token);
        var update = await SendAsync(HttpMethod.Patch, $"{Deliveries}/{parcelId}/status", token,
            JsonContent.Create(new { status = ParcelStatuses.PickedUp }));
        var history = await client.GetWithTokenAsync($"/api/v1/parcels/{parcelId}/history", token);
        var list = await client.GetWithTokenAsync(Deliveries, token);

        Assert.Equal(HttpStatusCode.Forbidden, view.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, history.StatusCode);
        Assert.DoesNotContain(parcelId.ToString(), await list.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Status_change_is_published_as_integration_event()
    {
        var (token, courierId) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId);

        var response = await SendAsync(HttpMethod.Patch, $"{Deliveries}/{parcelId}/status", token,
            JsonContent.Create(new { status = ParcelStatuses.PickedUp, note = "Collected" }));
        response.EnsureSuccessStatusCode();

        // The outbox processor publishes in the background, so wait for it.
        var published = await WaitForEventAsync(parcelId);
        Assert.Equal(ParcelEventRoutingKeys.StatusChanged, published.RoutingKey);
        var evt = JsonSerializer.Deserialize<ParcelStatusChangedIntegrationEvent>(published.Payload, JsonSerializerOptions.Web)!;
        Assert.Equal(ParcelStatuses.Created, evt.PreviousStatus);
        Assert.Equal(ParcelStatuses.PickedUp, evt.NewStatus);
        Assert.Equal(courierId, evt.CourierId);
        Assert.Equal(published.EventId, evt.EventId);
    }

    [Fact]
    public async Task Courier_completes_delivery_with_photo_proof()
    {
        var (token, courierId) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId);
        foreach (var status in new[] { ParcelStatuses.PickedUp, ParcelStatuses.InTransit, ParcelStatuses.OutForDelivery })
        {
            var step = await SendAsync(HttpMethod.Patch, $"{Deliveries}/{parcelId}/status", token,
                JsonContent.Create(new { status }));
            step.EnsureSuccessStatusCode();
        }

        var photo = new ByteArrayContent(PngBytes);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent
        {
            { new StringContent("Nimal Silva"), "recipientName" },
            { photo, "photo", "doorstep.png" },
        };
        var delivered = await SendAsync(HttpMethod.Post, $"{Deliveries}/{parcelId}/deliver", token, form);
        Assert.True(delivered.IsSuccessStatusCode, await delivered.Content.ReadAsStringAsync());

        var adminToken = await client.LoginAsync("admin");
        var stored = await client.GetWithTokenAsync($"/api/v1/parcels/{parcelId}/proof/photo", adminToken);
        Assert.Equal(HttpStatusCode.OK, stored.StatusCode);
        Assert.Equal("image/png", stored.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await stored.Content.ReadAsByteArrayAsync());

        // A customer who did not book this parcel cannot see the proof.
        var customerToken = await client.LoginAsync("customer1");
        var denied = await client.GetWithTokenAsync($"/api/v1/parcels/{parcelId}/proof", customerToken);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task Failure_with_reason_other_needs_a_note()
    {
        var (token, courierId) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId);

        var response = await SendAsync(HttpMethod.Post, $"{Deliveries}/{parcelId}/fail", token,
            new MultipartFormDataContent { { new StringContent(FailureReasons.Other), "reason" } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("Other", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Courier_reports_failure_with_attempt_photo()
    {
        var (token, courierId) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId);

        var photo = new ByteArrayContent(PngBytes);
        photo.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var response = await SendAsync(HttpMethod.Post, $"{Deliveries}/{parcelId}/fail", token,
            new MultipartFormDataContent
            {
                { new StringContent(FailureReasons.AccessRestricted), "reason" },
                { new StringContent("Gate locked"), "note" },
                { photo, "photo", "gate.png" },
            });
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());

        var stored = await client.GetWithTokenAsync($"/api/v1/parcels/{parcelId}/failure/photo", token);
        Assert.Equal(HttpStatusCode.OK, stored.StatusCode);
        Assert.Equal(PngBytes, await stored.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task Skipping_a_stage_returns_409()
    {
        var (token, courierId) = await LoginCourierAsync();
        var parcelId = await SeedAssignedParcelAsync(courierId);

        var response = await SendAsync(HttpMethod.Patch, $"{Deliveries}/{parcelId}/status", token,
            JsonContent.Create(new { status = ParcelStatuses.OutForDelivery }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<(string Token, Guid CourierId)> LoginCourierAsync()
    {
        var response = await client.PostLoginAsync("courier1", "courier1");
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return (body.GetProperty("accessToken").GetString()!, body.GetProperty("user").GetProperty("id").GetGuid());
    }

    /// Books a parcel straight into the Parcels database and assigns it through the
    /// public IParcelAssignment contract, the same call Dispatch makes.
    private async Task<Guid> SeedAssignedParcelAsync(Guid courierId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var parcel = new Parcel(Guid.NewGuid(), $"SD-IT-{Guid.NewGuid():N}"[..18], Guid.NewGuid(),
            "12 Galle Road, Colombo 03", "Sender Perera", "+94771234567",
            "45 Temple Street, Kandy", "Nimal Silva", "+94779876543",
            1.5m, "Small", 750m, "COD");
        db.Parcels.Add(parcel);
        await db.SaveChangesAsync();

        var assigned = await scope.ServiceProvider.GetRequiredService<IParcelAssignment>()
            .AssignCourierAsync(parcel.Id, courierId, CancellationToken.None);
        Assert.True(assigned);
        return parcel.Id;
    }

    private async Task<(Guid EventId, string RoutingKey, string Payload)> WaitForEventAsync(Guid parcelId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            var match = factory.Events.Published.FirstOrDefault(e => e.Payload.Contains(parcelId.ToString()));
            if (match != default) return match;
            await Task.Delay(100);
        }
        throw new TimeoutException("The status change event was not published within 10 seconds.");
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, HttpContent content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }
}
