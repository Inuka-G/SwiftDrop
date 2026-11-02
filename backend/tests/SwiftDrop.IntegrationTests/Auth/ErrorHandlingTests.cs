using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SwiftDrop.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class ErrorHandlingTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Unexpected_error_returns_500_and_hides_the_details()
    {
        var response = await _client.GetAsync("/api/v1/test-errors/unexpected");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.DoesNotContain("super-secret", body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);
    }

    [Fact]
    public async Task Expected_error_keeps_its_status_and_message()
    {
        var response = await _client.GetAsync("/api/v1/test-errors/not-found");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Parcel not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unknown_url_returns_404_in_the_standard_error_format()
    {
        var response = await _client.GetAsync("/api/v1/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
