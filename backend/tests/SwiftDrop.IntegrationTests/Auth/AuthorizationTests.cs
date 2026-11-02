using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace SwiftDrop.IntegrationTests.Auth;

[Collection(ApiCollection.Name)]
public class AuthorizationTests(SwiftDropApiFactory factory)
{
    private const string CourierOnly = "/api/v1/auth/role-check/courier";

    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("customer1", "customer", HttpStatusCode.OK)]
    [InlineData("customer1", "merchant", HttpStatusCode.Forbidden)]
    [InlineData("customer1", "courier", HttpStatusCode.Forbidden)]
    [InlineData("customer1", "dispatcher-or-admin", HttpStatusCode.Forbidden)]
    [InlineData("merchant1", "customer", HttpStatusCode.Forbidden)]
    [InlineData("merchant1", "merchant", HttpStatusCode.OK)]
    [InlineData("merchant1", "courier", HttpStatusCode.Forbidden)]
    [InlineData("merchant1", "dispatcher-or-admin", HttpStatusCode.Forbidden)]
    [InlineData("courier1", "customer", HttpStatusCode.Forbidden)]
    [InlineData("courier1", "merchant", HttpStatusCode.Forbidden)]
    [InlineData("courier1", "courier", HttpStatusCode.OK)]
    [InlineData("courier1", "dispatcher-or-admin", HttpStatusCode.Forbidden)]
    [InlineData("admin", "customer", HttpStatusCode.Forbidden)]
    [InlineData("admin", "merchant", HttpStatusCode.Forbidden)]
    [InlineData("admin", "courier", HttpStatusCode.Forbidden)]
    [InlineData("admin", "dispatcher-or-admin", HttpStatusCode.OK)]
    public async Task Each_role_reaches_only_its_own_endpoints(string username, string endpoint, HttpStatusCode expected)
    {
        var token = await _client.LoginAsync(username);

        var response = await _client.GetWithTokenAsync($"/api/v1/auth/role-check/{endpoint}", token);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task No_token_returns_401_in_the_standard_error_format()
    {
        var response = await _client.GetAsync(CourierOnly);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Wrong_role_returns_403_in_the_standard_error_format()
    {
        var token = await _client.LoginAsync("customer1");

        var response = await _client.GetWithTokenAsync(CourierOnly, token);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Garbage_token_returns_401()
    {
        var response = await _client.GetWithTokenAsync(CourierOnly, "not-a-real-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_forged_with_another_key_returns_401()
    {
        var forged = MakeToken(new string('x', 48), DateTime.UtcNow.AddHours(1));

        var response = await _client.GetWithTokenAsync(CourierOnly, forged);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Expired_token_returns_401()
    {
        var expired = MakeToken(RealKey(), DateTime.UtcNow.AddHours(-1));

        var response = await _client.GetWithTokenAsync(CourierOnly, expired);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private string RealKey() => factory.Services.GetRequiredService<IConfiguration>()["Jwt:Key"]!;

    private static string MakeToken(string key, DateTime expires)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "SwiftDrop",
            Audience = "SwiftDrop.Client",
            NotBefore = expires.AddHours(-2),
            Expires = expires,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", "COURIER")]),
        });
    }
}
