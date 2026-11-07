using System.Net;
using SwiftDrop.IntegrationTests.Auth;

namespace SwiftDrop.IntegrationTests.Dispatch;

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
}
