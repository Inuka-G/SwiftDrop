using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SwiftDrop.IntegrationTests.Auth;

namespace SwiftDrop.IntegrationTests.Reporting;

[Collection(ApiCollection.Name)]
public sealed class AdminDashboardAuthorizationTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();
    private static readonly string[] Endpoints =
    [
        "/api/v1/dispatch/summary",
        "/api/v1/reports/summary",
        "/api/v1/reports/deliveries-per-day?from=2026-01-01&to=2026-01-07",
        "/api/v1/reports/success-failure-rate?from=2026-01-01&to=2026-01-07"
    ];

    [Theory]
    [MemberData(nameof(EndpointCases))]
    public async Task Dashboard_routes_return_401_without_login(string endpoint) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(endpoint)).StatusCode);

    public static IEnumerable<object[]> EndpointCases => Endpoints.Select(endpoint => new object[] { endpoint });

    [Theory]
    [InlineData("customer1")]
    [InlineData("merchant1")]
    [InlineData("courier1")]
    public async Task Dashboard_routes_return_403_for_non_dispatcher_roles(string username)
    {
        var token = await client.LoginAsync(username);
        foreach (var endpoint in Endpoints)
            Assert.Equal(HttpStatusCode.Forbidden, (await client.GetWithTokenAsync(endpoint, token)).StatusCode);
    }

    [Theory]
    [InlineData("DISPATCHER")]
    [InlineData("ADMIN")]
    public async Task Dashboard_routes_allow_dispatcher_and_admin(string role)
    {
        var token = CreateRoleToken(role);
        foreach (var endpoint in Endpoints)
        {
            var response = await client.GetWithTokenAsync(endpoint, token);
            Assert.True(response.StatusCode == HttpStatusCode.OK,
                $"{endpoint}: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }
    }

    private string CreateRoleToken(string role)
    {
        var key = factory.Services.GetRequiredService<IConfiguration>()["Jwt:Key"]!;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "SwiftDrop",
            Audience = "SwiftDrop.Client",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", role)])
        });
    }
}
