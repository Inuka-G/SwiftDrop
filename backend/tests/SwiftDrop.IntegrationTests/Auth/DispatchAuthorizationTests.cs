using System.Net;

namespace SwiftDrop.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public sealed class DispatchAuthorizationTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();
    private const string UnassignedParcels = "/api/v1/dispatch/parcels/unassigned";

    [Fact]
    public async Task Unauthenticated_request_returns_401()
    {
        var response = await client.GetAsync(UnassignedParcels);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("customer1")]
    [InlineData("merchant1")]
    [InlineData("courier1")]
    public async Task Non_dispatcher_roles_are_forbidden(string username)
    {
        var token = await client.LoginAsync(username);
        var response = await client.GetWithTokenAsync(UnassignedParcels, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dispatcher_or_admin_can_reach_dispatch_endpoint()
    {
        var token = await client.LoginAsync("admin");
        var response = await client.GetWithTokenAsync(UnassignedParcels, token);
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"{(int)response.StatusCode}: {body}");
    }

    [Fact]
    public async Task Swagger_documents_courier_endpoints()
    {
        var swagger = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/dispatch/couriers", swagger);
        Assert.Contains("/api/v1/dispatch/couriers/summary", swagger);
        Assert.Contains("/api/v1/dispatch/couriers/{courierId}", swagger);
    }

    [Theory]
    [InlineData("GET", "/api/v1/dispatch/couriers")]
    [InlineData("GET", "/api/v1/dispatch/couriers/summary")]
    [InlineData("POST", "/api/v1/dispatch/couriers")]
    [InlineData("PATCH", "/api/v1/dispatch/couriers/00000000-0000-0000-0000-000000000001")]
    public async Task Every_courier_endpoint_requires_login(string method, string path)
    {
        using var response = await SendAsync(method, path, null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("customer1")]
    [InlineData("merchant1")]
    [InlineData("courier1")]
    public async Task Every_courier_endpoint_forbids_non_dispatcher_roles(string username)
    {
        var token = await client.LoginAsync(username);
        foreach (var (method, path) in CourierEndpoints)
        {
            using var response = await SendAsync(method, path, token);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    private static readonly (string Method, string Path)[] CourierEndpoints =
    [
        ("GET", "/api/v1/dispatch/couriers"),
        ("GET", "/api/v1/dispatch/couriers/summary"),
        ("POST", "/api/v1/dispatch/couriers"),
        ("PATCH", "/api/v1/dispatch/couriers/00000000-0000-0000-0000-000000000001")
    ];

    private async Task<HttpResponseMessage> SendAsync(string method, string path, string? token)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (token is not null) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        if (method == "POST") request.Content = System.Net.Http.Json.JsonContent.Create(new
        {
            courierId = Guid.NewGuid(), zoneId = (Guid?)null, vehicleType = "Van", licensePlate = "TEST-1"
        });
        if (method == "PATCH") request.Content = System.Net.Http.Json.JsonContent.Create(new { isAvailable = false });
        return await client.SendAsync(request);
    }
}
