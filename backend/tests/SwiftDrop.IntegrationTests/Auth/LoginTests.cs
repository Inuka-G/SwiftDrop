using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SwiftDrop.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class LoginTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("admin", "ADMIN")]
    [InlineData("customer1", "CUSTOMER")]
    [InlineData("courier1", "COURIER")]
    [InlineData("merchant1", "MERCHANT")]
    public async Task Each_report_login_works_and_gets_its_role(string username, string role)
    {
        var token = await _client.LoginAsync(username);

        var response = await _client.GetWithTokenAsync("/api/v1/auth/me", token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(role, me.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Wrong_password_returns_401_in_the_standard_error_format()
    {
        var response = await _client.PostLoginAsync("admin", "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Missing_password_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new { username = "admin" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Signup_cannot_choose_a_role_and_the_new_user_can_log_in()
    {
        var email = $"{Guid.NewGuid():N}@test.local";
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup",
            new { fullName = "New Customer", email, password = "Secret123!", role = "ADMIN" });

        Assert.Equal(HttpStatusCode.Created, signup.StatusCode);
        var created = await signup.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("CUSTOMER", created.GetProperty("user").GetProperty("role").GetString());

        var login = await _client.PostLoginAsync(email, "Secret123!");
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Signup_with_a_short_password_returns_400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup",
            new { fullName = "Someone", email = $"{Guid.NewGuid():N}@test.local", password = "short" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Signup_with_a_used_email_returns_409()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/signup",
            new { fullName = "Copy", email = "admin@swiftdrop.local", password = "Secret123!" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
