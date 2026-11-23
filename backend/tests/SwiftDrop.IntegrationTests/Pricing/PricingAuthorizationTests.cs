using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SwiftDrop.IntegrationTests.Auth;

namespace SwiftDrop.IntegrationTests.Pricing;

[Collection(ApiCollection.Name)]
public sealed class PricingAuthorizationTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();
    private static readonly Guid MissingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    public static IEnumerable<object[]> ManagementRequests =>
    [
        ["POST", "/api/v1/zones", new { zoneName = "Authorization Test Zone", description = "Temporary test zone" }],
        ["POST", $"/api/v1/zones/{MissingId}/pricing-rules", new { serviceType = 1, baseRate = 1m, additionalKgRate = 1m, isActive = true }],
        ["PUT", $"/api/v1/zones/{MissingId}", new { zoneName = "Authorization Test Zone", description = "Temporary test zone" }],
        ["PATCH", $"/api/v1/zones/{MissingId}/status", new { isActive = false }],
        ["PUT", $"/api/v1/zones/{MissingId}/pricing-rules/{MissingId}", new { baseRate = 1m, additionalKgRate = 1m }],
        ["GET", $"/api/v1/zones/{MissingId}/pricing-rules", null!],
    ];

    [Theory]
    [MemberData(nameof(ManagementRequests))]
    public async Task Management_requests_require_authentication(string method, string path, object? body)
    {
        using var response = await SendAsync(method, path, body);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("CUSTOMER")]
    [InlineData("MERCHANT")]
    [InlineData("COURIER")]
    [InlineData("DISPATCHER")]
    public async Task Management_requests_forbid_non_admin_roles(string role)
    {
        var token = CreateRoleToken(role);
        foreach (var (method, path, body) in ManagementRequests.Select(row => ((string)row[0], (string)row[1], row[2])))
        {
            using var response = await SendAsync(method, path, body, token);
            Assert.True(response.StatusCode == HttpStatusCode.Forbidden,
                $"{method} {path} returned {(int)response.StatusCode}, expected 403.");
        }
    }

    [Fact]
    public async Task Admin_can_manage_zones_and_pricing_rules_while_booking_reads_remain_public()
    {
        var adminToken = CreateRoleToken("ADMIN");
        var zonesResponse = await client.GetAsync("/api/v1/zones");
        Assert.Equal(HttpStatusCode.OK, zonesResponse.StatusCode);
        var zones = await zonesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var existingZoneId = zones[0].GetProperty("id").GetGuid();

        using var detailResponse = await client.GetAsync($"/api/v1/zones/{existingZoneId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        using var estimateResponse = await client.GetAsync($"/api/v1/zones/{existingZoneId}/estimate?weightKg=1&serviceType=1");
        Assert.Equal(HttpStatusCode.OK, estimateResponse.StatusCode);

        using var rulesResponse = await SendAsync("GET", $"/api/v1/zones/{existingZoneId}/pricing-rules", null, adminToken);
        Assert.Equal(HttpStatusCode.OK, rulesResponse.StatusCode);
        var rules = await rulesResponse.Content.ReadFromJsonAsync<JsonElement>();
        var rule = rules[0];
        var ruleId = rule.GetProperty("id").GetGuid();

        using var createResponse = await SendAsync("POST", "/api/v1/zones", new
        {
            zoneName = $"Authorization Test {Guid.NewGuid():N}",
            description = "Created by isolated authorization test",
        }, adminToken);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdZone = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var createdZoneId = createdZone.GetProperty("id").GetGuid();

        using var updateResponse = await SendAsync("PUT", $"/api/v1/zones/{createdZoneId}", new
        {
            zoneName = createdZone.GetProperty("zoneName").GetString(),
            description = "Updated by isolated authorization test",
        }, adminToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        using var statusResponse = await SendAsync("PATCH", $"/api/v1/zones/{createdZoneId}/status", new { isActive = false }, adminToken);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);

        using var standardCreateResponse = await SendAsync("POST", $"/api/v1/zones/{createdZoneId}/pricing-rules", new
        {
            serviceType = 1,
            baseRate = 350m,
            additionalKgRate = 80m,
            isActive = true,
        }, adminToken);
        Assert.Equal(HttpStatusCode.Created, standardCreateResponse.StatusCode);

        using var expressCreateResponse = await SendAsync("POST", $"/api/v1/zones/{createdZoneId}/pricing-rules", new
        {
            serviceType = 2,
            baseRate = 600m,
            additionalKgRate = 120m,
            isActive = true,
        }, adminToken);
        Assert.Equal(HttpStatusCode.Created, expressCreateResponse.StatusCode);

        using var duplicateResponse = await SendAsync("POST", $"/api/v1/zones/{createdZoneId}/pricing-rules", new
        {
            serviceType = 1,
            baseRate = 350m,
            additionalKgRate = 80m,
            isActive = true,
        }, adminToken);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);

        using var invalidTypeResponse = await SendAsync("POST", $"/api/v1/zones/{createdZoneId}/pricing-rules", new
        {
            serviceType = 3,
            baseRate = 350m,
            additionalKgRate = 80m,
            isActive = true,
        }, adminToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidTypeResponse.StatusCode);

        using var invalidRateResponse = await SendAsync("POST", $"/api/v1/zones/{createdZoneId}/pricing-rules", new
        {
            serviceType = 1,
            baseRate = -1m,
            additionalKgRate = 80m,
            isActive = true,
        }, adminToken);
        Assert.Equal(HttpStatusCode.BadRequest, invalidRateResponse.StatusCode);

        using var missingZoneResponse = await SendAsync("POST", $"/api/v1/zones/{MissingId}/pricing-rules", new
        {
            serviceType = 1,
            baseRate = 350m,
            additionalKgRate = 80m,
            isActive = true,
        }, adminToken);
        Assert.Equal(HttpStatusCode.NotFound, missingZoneResponse.StatusCode);

        using var rateResponse = await SendAsync("PUT", $"/api/v1/zones/{existingZoneId}/pricing-rules/{ruleId}", new
        {
            baseRate = rule.GetProperty("baseRate").GetDecimal(),
            additionalKgRate = rule.GetProperty("additionalKgRate").GetDecimal(),
        }, adminToken);
        Assert.Equal(HttpStatusCode.OK, rateResponse.StatusCode);
    }

    private async Task<HttpResponseMessage> SendAsync(string method, string path, object? body, string? token = null)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (body is not null) request.Content = JsonContent.Create(body);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
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
