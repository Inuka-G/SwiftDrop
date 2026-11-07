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
public sealed class ReportingAuthorizationTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();
    public static IEnumerable<object[]> ReportEndpoints => new[]
    {
        new object[] { "/api/v1/reports/summary" },
        new object[] { "/api/v1/reports/deliveries-per-day?from=2026-06-01&to=2026-06-01" },
        new object[] { "/api/v1/reports/success-failure-rate?from=2026-06-01&to=2026-06-01" }
    };

    [Theory]
    [MemberData(nameof(ReportEndpoints))]
    public async Task Unauthenticated_request_returns_401(string endpoint) =>
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(endpoint)).StatusCode);

    [Theory]
    [MemberData(nameof(NonDispatcherEndpointCases))]
    public async Task Non_dispatcher_roles_are_forbidden_on_all_endpoints(string username, string endpoint)
    {
        var response = await client.GetWithTokenAsync(endpoint, await client.LoginAsync(username));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static IEnumerable<object[]> NonDispatcherEndpointCases =>
        from username in new[] { "customer1", "merchant1", "courier1" }
        from endpoint in ReportEndpoints.Select(row => (string)row[0])
        select new object[] { username, endpoint };

    [Theory]
    [MemberData(nameof(ReportEndpoints))]
    public async Task Dispatcher_and_admin_can_read_all_endpoints(string endpoint)
    {
        var adminResponse = await client.GetWithTokenAsync(endpoint, await client.LoginAsync("admin"));
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);

        var dispatcherResponse = await client.GetWithTokenAsync(endpoint, DispatcherToken());
        Assert.Equal(HttpStatusCode.OK, dispatcherResponse.StatusCode);
    }

    private string DispatcherToken()
    {
        var key = factory.Services.GetRequiredService<IConfiguration>()["Jwt:Key"]!;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "SwiftDrop",
            Audience = "SwiftDrop.Client",
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity([new Claim("sub", Guid.NewGuid().ToString()), new Claim("role", "DISPATCHER")])
        });
    }
}
