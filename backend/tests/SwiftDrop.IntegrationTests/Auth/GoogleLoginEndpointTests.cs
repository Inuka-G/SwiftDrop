using System.Net;
using System.Net.Http.Json;

namespace SwiftDrop.IntegrationTests.Auth;

/// The real Google round trip needs a browser and a Google account, so it is tested by hand.
/// These tests cover what the server must do on its own.
[Collection(ApiCollection.Name)]
public class GoogleLoginEndpointTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Google_login_without_secrets_returns_503_in_the_standard_error_format()
    {
        var response = await _client.GetAsync("/api/v1/auth/google/login");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Exchanging_a_made_up_code_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/exchange", new { code = "made-up-code" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Exchanging_without_a_code_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/google/exchange", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
