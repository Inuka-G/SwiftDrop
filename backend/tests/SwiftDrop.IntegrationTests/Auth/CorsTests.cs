using System.Net;

namespace SwiftDrop.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class CorsTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private Task<HttpResponseMessage> PreflightAsync(string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/login");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task React_app_origin_is_allowed()
    {
        var response = await PreflightAsync("http://localhost:5173");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Unknown_origin_is_not_allowed()
    {
        var response = await PreflightAsync("http://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
