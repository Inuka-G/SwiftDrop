using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.IntegrationTests.Auth;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;
using SwiftDrop.Pricing.Data.Persistence;

namespace SwiftDrop.IntegrationTests.Parcels;

[Collection(ApiCollection.Name)]
public sealed class MerchantOrdersTests(SwiftDropApiFactory factory)
{
    private const string Orders = "/api/v1/merchant/orders";
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-key")]
    public async Task Missing_or_invalid_api_key_returns_401(string? key)
    {
        using var response = await PostAsync(key, Booking(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Revoked_api_key_returns_401()
    {
        using var scope = factory.Services.CreateScope();
        var merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();
        var (merchantId, key) = await CreateKeyAsync(merchants);
        await merchants.RevokeApiKeyAsync(merchantId, key.Id);

        using var response = await PostAsync(key.PlainTextKey, Booking(Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var list = await GetAsync(Orders, key.PlainTextKey);
        using var detail = await GetAsync($"{Orders}/{Guid.NewGuid()}", key.PlainTextKey);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detail.StatusCode);
    }

    [Fact]
    public async Task Valid_key_with_invalid_booking_returns_400()
    {
        using var scope = factory.Services.CreateScope();
        var (_, key) = await CreateKeyAsync(scope.ServiceProvider.GetRequiredService<IMerchantService>());
        using var response = await PostAsync(key.PlainTextKey, Booking(Guid.NewGuid()) with { WeightKg = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Weight", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Valid_key_creates_trackable_parcel_owned_by_the_key_merchant()
    {
        using var scope = factory.Services.CreateScope();
        var merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();
        var (merchantId, key) = await CreateKeyAsync(merchants);
        var pricing = scope.ServiceProvider.GetRequiredService<PricingDbContext>();
        var zoneId = await pricing.Zones.Where(z => z.ZoneName == "Colombo").Select(z => z.Id).SingleAsync();
        var body = JsonSerializer.SerializeToNode(Booking(zoneId), JsonSerializerOptions.Web)!.AsObject();
        var otherMerchantId = Guid.NewGuid();
        body["merchantId"] = otherMerchantId.ToString();
        body["customerId"] = Guid.NewGuid().ToString();

        using var response = await PostAsync(key.PlainTextKey, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ParcelCreatedResponse>();
        Assert.NotNull(created);
        Assert.Equal(430m, created.EstimatedFee);
        Assert.Equal("Created", created.Status);
        Assert.Contains(created.TrackingNumber, response.Headers.Location!.ToString());

        using var tracking = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, tracking.StatusCode);

        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var parcel = await db.Parcels.SingleAsync(p => p.Id == created.ParcelId);
        Assert.Equal(merchantId, parcel.MerchantId);
        Assert.Null(parcel.CustomerId);
        var history = await db.StatusHistory.SingleAsync(h => h.ParcelId == parcel.Id);
        Assert.Equal("Created", history.ToStatus);
        Assert.Equal(merchantId, history.ChangedByActorId);
        Assert.Equal(ParcelStatusActorTypes.Merchant, history.ChangedByActorType);

        var rows = await db.OutboxMessages.Where(m => m.Payload.Contains(parcel.Id.ToString())).ToListAsync();
        var createdEvent = JsonSerializer.Deserialize<ParcelCreatedIntegrationEvent>(
            Assert.Single(rows, m => m.RoutingKey == ParcelEventRoutingKeys.Created).Payload, JsonSerializerOptions.Web)!;
        Assert.Equal(merchantId, createdEvent.MerchantId);
        Assert.Null(createdEvent.CustomerId);
        var statusEvent = JsonSerializer.Deserialize<ParcelStatusChangedIntegrationEvent>(
            Assert.Single(rows, m => m.RoutingKey == ParcelEventRoutingKeys.StatusChanged).Payload, JsonSerializerOptions.Web)!;
        Assert.Equal(merchantId, statusEvent.MerchantId);
        Assert.Null(statusEvent.CustomerId);
    }

    private async Task<HttpResponseMessage> PostAsync(string? key, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, Orders)
        {
            Content = JsonContent.Create(body)
        };
        if (key is not null)
            request.Headers.Add("X-Api-Key", key);
        return await client.SendAsync(request);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("invalid-key")]
    public async Task Retrieval_requires_a_valid_key(string? key)
    {
        using var list = await GetAsync(Orders, key);
        using var detail = await GetAsync($"{Orders}/{Guid.NewGuid()}", key);
        Assert.Equal(HttpStatusCode.Unauthorized, list.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, detail.StatusCode);
    }

    [Fact]
    public async Task Retrieval_pages_filters_and_scopes_orders_by_merchant()
    {
        using var scope = factory.Services.CreateScope();
        var merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();
        var (merchantId, key) = await CreateKeyAsync(merchants);
        var (otherMerchantId, _) = await CreateKeyAsync(merchants);
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var older = NewParcel(merchantId, null, "Delivered");
        var newer = NewParcel(merchantId, null, "Created");
        var foreign = NewParcel(otherMerchantId, null, "Delivered");
        var customer = NewParcel(null, Guid.NewGuid(), "Delivered");
        db.AddRange(older, newer, foreign, customer);
        var now = DateTime.UtcNow;
        db.Entry(older).Property(p => p.CreatedAtUtc).CurrentValue = now.AddDays(-1);
        db.Entry(newer).Property(p => p.CreatedAtUtc).CurrentValue = now;
        await db.SaveChangesAsync();

        var first = await GetPageAsync("?pageSize=1", key.PlainTextKey);
        Assert.Equal((1, 1, 2), (first.Page, first.PageSize, first.TotalCount));
        Assert.Equal(newer.Id, Assert.Single(first.Items).ParcelId);
        var second = await GetPageAsync("?page=2&pageSize=1", key.PlainTextKey);
        Assert.Equal(older.Id, Assert.Single(second.Items).ParcelId);
        Assert.Empty((await GetPageAsync("?page=3&pageSize=1", key.PlainTextKey)).Items);
        var delivered = await GetPageAsync("?status=delivered", key.PlainTextKey);
        Assert.Equal(1, delivered.TotalCount);
        Assert.Equal(older.Id, Assert.Single(delivered.Items).ParcelId);

        using var detail = await GetAsync($"{Orders}/{older.Id}", key.PlainTextKey);
        detail.EnsureSuccessStatusCode();
        var order = await detail.Content.ReadFromJsonAsync<MerchantOrderDto>();
        Assert.Equal(new MerchantOrderDto(older.Id, older.TrackingNumber, older.RecipientName,
            older.RecipientPhone, older.DropoffAddress, older.CurrentStatus, older.EstimatedFee, older.CreatedAtUtc), order);

        foreach (var hiddenId in new[] { foreign.Id, customer.Id, Guid.NewGuid() })
        {
            using var hidden = await GetAsync($"{Orders}/{hiddenId}", key.PlainTextKey);
            Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        }

        foreach (var invalidQuery in new[] { "?page=0", "?pageSize=101", "?page=2147483647", "?status=unknown" })
        {
            using var invalid = await GetAsync(Orders + invalidQuery, key.PlainTextKey);
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
    }

    private async Task<MerchantOrdersPage> GetPageAsync(string query, string key)
    {
        using var response = await GetAsync(Orders + query, key);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MerchantOrdersPage>())!;
    }

    [Fact]
    public async Task Browser_JWT_requires_an_approved_profile_and_only_returns_its_orders()
    {
        var email = $"merchant_user_{Guid.NewGuid():N}@example.test";
        var signupResponse = await client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            fullName = "Dashboard Merchant User",
            email,
            password = "SecurePass123!",
            phoneNumber = "+15555550199"
        });
        signupResponse.EnsureSuccessStatusCode();
        var signupBody = await signupResponse.Content.ReadFromJsonAsync<JsonElement>();
        var browserToken = signupBody.GetProperty("accessToken").GetString()!;

        var unlinkedToken = await client.LoginAsync("customer1");
        using var unlinked = await client.GetWithTokenAsync(Orders, unlinkedToken);
        Assert.Equal(HttpStatusCode.Forbidden, unlinked.StatusCode);

        using var registration = new HttpRequestMessage(HttpMethod.Post, "/api/v1/merchants/register")
        {
            Content = JsonContent.Create(new RegisterMerchantCommand("Dashboard Store", $"BR-{Guid.NewGuid():N}", null))
        };
        registration.Headers.Authorization = new AuthenticationHeaderValue("Bearer", browserToken);
        using var registered = await client.SendAsync(registration);
        registered.EnsureSuccessStatusCode();
        var profile = (await registered.Content.ReadFromJsonAsync<MerchantDto>())!;
        using var pending = await client.GetWithTokenAsync(Orders, browserToken);
        Assert.Equal(HttpStatusCode.Forbidden, pending.StatusCode);

        using var scope = factory.Services.CreateScope();
        var merchants = scope.ServiceProvider.GetRequiredService<IMerchantService>();
        await merchants.ApproveAsync(profile.Id, Guid.NewGuid());
        var (otherMerchantId, _) = await CreateKeyAsync(merchants);
        var db = scope.ServiceProvider.GetRequiredService<ParcelsDbContext>();
        var owned = NewParcel(profile.Id, null, "Created");
        var foreign = NewParcel(otherMerchantId, null, "Created");
        db.AddRange(owned, foreign);
        await db.SaveChangesAsync();

        using var list = await client.GetWithTokenAsync(Orders, browserToken);
        list.EnsureSuccessStatusCode();
        var page = (await list.Content.ReadFromJsonAsync<MerchantOrdersPage>())!;
        Assert.Equal(owned.Id, Assert.Single(page.Items).ParcelId);
        using var detail = await client.GetWithTokenAsync($"{Orders}/{owned.Id}", browserToken);
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var hidden = await client.GetWithTokenAsync($"{Orders}/{foreign.Id}", browserToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        using var mixed = new HttpRequestMessage(HttpMethod.Get, Orders);
        mixed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", browserToken);
        mixed.Headers.Add("X-Api-Key", "invalid-key");
        using var invalid = await client.SendAsync(mixed);
        Assert.Equal(HttpStatusCode.Unauthorized, invalid.StatusCode);

        var credentials = await merchants.CreateClientCredentialAsync(profile.Id, new CreateClientCredentialCommand("Test client"));
        var machineToken = await merchants.ExchangeClientCredentialsAsync(new OAuthTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = credentials.ClientId,
            ClientSecret = credentials.PlainTextClientSecret
        });
        using var machine = await client.GetWithTokenAsync(Orders, machineToken.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, machine.StatusCode);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string? key)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (key is not null)
            request.Headers.Add("X-Api-Key", key);
        return await client.SendAsync(request);
    }

    private static Parcel NewParcel(Guid? merchantId, Guid? customerId, string status)
    {
        var booking = Booking(Guid.NewGuid());
        var parcel = new Parcel(customerId, $"SD-{Guid.NewGuid():N}"[..20], booking.ZoneId, booking.PickupAddress,
            booking.PickupContactName, booking.PickupContactPhone, booking.DropoffAddress, booking.RecipientName,
            booking.RecipientPhone, booking.WeightKg, booking.SizeCategory, 430m, booking.PaymentMethod, merchantId);
        parcel.ChangeStatus(status, DateTime.UtcNow);
        return parcel;
    }

    private static CreateParcelRequest Booking(Guid zoneId) => new(zoneId,
        "1 Main St, Colombo", "Sender", "+94771234567",
        "2 Hill St, Colombo", "Recipient", "+94779876543", 2m, "Small");

    private static async Task<(Guid MerchantId, CreatedApiKeyDto Key)> CreateKeyAsync(IMerchantService merchants)
    {
        var registration = await merchants.RegisterAsync(new RegisterMerchantCommand(
            "Test Store", $"BR-{Guid.NewGuid():N}", null), null);
        await merchants.ApproveAsync(registration.Id, Guid.NewGuid());
        var key = await merchants.CreateApiKeyAsync(registration.Id, new CreateApiKeyCommand("Test key"));
        return (registration.Id, key);
    }
}
