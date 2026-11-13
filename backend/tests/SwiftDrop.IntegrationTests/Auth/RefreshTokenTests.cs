using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SwiftDrop.IntegrationTests.Auth;

/// The refresh token travels in an HttpOnly cookie. Cookies are handled by hand here,
/// so each test can see exactly which cookie the server sets and which one is sent back.
[Collection(ApiCollection.Name)]
public class RefreshTokenTests(SwiftDropApiFactory factory)
{
    private const string CookieName = "swiftdrop_refresh";

    private readonly HttpClient _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string? GetSetCookie(HttpResponseMessage response) =>
        response.Headers.TryGetValues("Set-Cookie", out var values)
            ? values.FirstOrDefault(v => v.StartsWith(CookieName + "="))
            : null;

    private static string CookieValue(string setCookie) => setCookie.Split(';')[0][(CookieName.Length + 1)..];

    private async Task<string> LoginAndGetRefreshTokenAsync(bool rememberMe = true)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "customer1", password = "customer1", rememberMe });
        response.EnsureSuccessStatusCode();
        return CookieValue(GetSetCookie(response)!);
    }

    private Task<HttpResponseMessage> PostWithCookieAsync(string url, string? refreshToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        if (refreshToken is not null) request.Headers.Add("Cookie", $"{CookieName}={refreshToken}");
        return _client.SendAsync(request);
    }

    [Fact]
    public async Task Login_sets_a_secure_HttpOnly_cookie_only_for_auth_endpoints()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "customer1", password = "customer1" });

        var cookie = GetSetCookie(response)!.ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("samesite=strict", cookie);
        Assert.Contains("path=/api/v1/auth", cookie);
        Assert.Contains("expires=", cookie);

        // The refresh token is only in the cookie, never in the JSON body that JavaScript can read.
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(CookieValue(GetSetCookie(response)!), body);
    }

    [Fact]
    public async Task Without_remember_me_the_cookie_ends_when_the_browser_closes()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { username = "customer1", password = "customer1", rememberMe = false });

        Assert.DoesNotContain("expires=", GetSetCookie(response)!.ToLowerInvariant());
    }

    [Fact]
    public async Task Refresh_returns_a_working_access_token_and_a_new_cookie()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var response = await PostWithCookieAsync("/api/v1/auth/refresh", refreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEqual(refreshToken, CookieValue(GetSetCookie(response)!));
        var accessToken = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var me = await _client.GetWithTokenAsync("/api/v1/auth/me", accessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task Used_refresh_token_is_rejected()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();
        await PostWithCookieAsync("/api/v1/auth/refresh", refreshToken);

        var response = await PostWithCookieAsync("/api/v1/auth/refresh", refreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Refresh_without_a_cookie_returns_401()
    {
        var response = await PostWithCookieAsync("/api/v1/auth/refresh", refreshToken: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task After_logout_the_refresh_token_no_longer_works()
    {
        var refreshToken = await LoginAndGetRefreshTokenAsync();

        var logout = await PostWithCookieAsync("/api/v1/auth/logout", refreshToken);
        var refresh = await PostWithCookieAsync("/api/v1/auth/refresh", refreshToken);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }
}
