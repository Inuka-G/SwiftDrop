using System.Net;
using System.Net.Http.Json;
using SwiftDrop.IntegrationTests.Auth;

namespace SwiftDrop.IntegrationTests.Pricing;

[Collection(ApiCollection.Name)]
public sealed class ZoneAuthorizationTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Theory]
    [InlineData("POST", "/api/v1/zones")]
    [InlineData("PUT", "/api/v1/zones/00000000-0000-0000-0000-000000000001")]
    [InlineData("PATCH", "/api/v1/zones/00000000-0000-0000-0000-000000000001/status")]
    [InlineData("PUT", "/api/v1/zones/00000000-0000-0000-0000-000000000001/pricing-rules/00000000-0000-0000-0000-000000000002")]
    public async Task Zone_and_pricing_mutations_reject_anonymous_requests(string method, string path)
    {
        using var response = await SendAsync(method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/zones")]
    [InlineData("PUT", "/api/v1/zones/00000000-0000-0000-0000-000000000001")]
    [InlineData("PATCH", "/api/v1/zones/00000000-0000-0000-0000-000000000001/status")]
    [InlineData("PUT", "/api/v1/zones/00000000-0000-0000-0000-000000000001/pricing-rules/00000000-0000-0000-0000-000000000002")]
    public async Task Zone_and_pricing_mutations_reject_non_admin_users(string method, string path)
    {
        var token = await client.LoginAsync("customer1");
        using var response = await SendAsync(method, path, token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(string method, string path, string? token = null)
    {
        var body = path.EndsWith("/status", StringComparison.Ordinal)
            ? JsonContent.Create(new { isActive = false })
            : path.Contains("pricing-rules", StringComparison.Ordinal)
                ? JsonContent.Create(new { baseRate = 100m, additionalKgRate = 10m })
                : path.EndsWith("/zones", StringComparison.Ordinal)
                    ? JsonContent.Create(new { zoneName = $"Test zone {Guid.NewGuid():N}", description = "Authorization test" })
                    : JsonContent.Create(new { zoneName = "Updated zone", description = "Authorization test" });
        using var request = new HttpRequestMessage(new HttpMethod(method), path) { Content = body };
        if (token is not null)
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }
}
