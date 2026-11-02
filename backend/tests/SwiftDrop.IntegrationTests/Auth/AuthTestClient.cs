using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SwiftDrop.IntegrationTests.Auth;

internal static class AuthTestClient
{
    public static Task<HttpResponseMessage> PostLoginAsync(this HttpClient client, string username, string password) =>
        client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });

    public static async Task<string> LoginAsync(this HttpClient client, string username)
    {
        var response = await client.PostLoginAsync(username, username);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    public static Task<HttpResponseMessage> GetWithTokenAsync(this HttpClient client, string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }
}
