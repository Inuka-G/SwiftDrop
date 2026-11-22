using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SwiftDrop.IntegrationTests.Auth;
using Xunit;

namespace SwiftDrop.IntegrationTests.Merchants;

[Collection(ApiCollection.Name)]
public sealed class MerchantIntegrationTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task RateLimiting_BurstingPastThreshold_Returns429TooManyRequests()
    {
        // Use an isolated client so exhausting the rate limiter here does not affect other tests
        var isolatedClient = factory.WithWebHostBuilder(_ => { }).CreateClient();
        var responses = new List<HttpResponseMessage>();

        for (int i = 0; i < 18; i++)
        {
            var res = await isolatedClient.PostAsJsonAsync("/api/v1/oauth/token", new { grant_type = "invalid" });
            responses.Add(res);
        }

        // At least one request beyond the limit must receive 429 Too Many Requests
        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ApiVersioning_EndpointIsAccessibleOnV1()
    {
        // Validates that the v1 route segment works under Asp.Versioning on a general endpoint
        var response = await client.PostAsJsonAsync("/api/v1/merchants/register", new { businessName = "" });
        
        // Endpoint exists and executes under v1 (returns 400 Bad Request for validation error, not 404)
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authorization_NonAdmin_CannotAccessPendingApprovalQueue()
    {
        // 1. Customer cannot view the admin approval queue (403 Forbidden)
        var customerToken = await client.LoginAsync("customer1");
        var customerResponse = await client.GetWithTokenAsync("/api/v1/merchants/pending", customerToken);
        Assert.Equal(HttpStatusCode.Forbidden, customerResponse.StatusCode);

        // 2. Admin can view the queue (200 OK)
        var adminToken = await client.LoginAsync("admin");
        var adminResponse = await client.GetWithTokenAsync("/api/v1/merchants/pending", adminToken);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task FullMerchantWorkflow_Register_Approve_CreateKeys_And_ExchangeToken()
    {
        // 1. Register a new merchant
        var uniqueReg = $"REG-{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        var merchantToken = await client.LoginAsync("merchant1");

        var registerRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/merchants/register")
        {
            Content = JsonContent.Create(new
            {
                businessName = "Nexora Retail Ltd",
                businessRegNumber = uniqueReg,
                webhookUrl = "https://example.com/webhook"
            })
        };
        registerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", merchantToken);
        var registerResponse = await client.SendAsync(registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var merchantJson = await registerResponse.Content.ReadFromJsonAsync<JsonElement>();
        var merchantId = merchantJson.GetProperty("id").GetGuid();

        // 2. Admin approves the merchant
        var adminToken = await client.LoginAsync("admin");
        var approveRequest = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/merchants/{merchantId}/approve");
        approveRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var approveResponse = await client.SendAsync(approveRequest);
        Assert.Equal(HttpStatusCode.OK, approveResponse.StatusCode);

        // 3. Merchant creates an API key
        var apiKeyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/merchants/api-keys")
        {
            Content = JsonContent.Create(new { name = "Production Store Key" })
        };
        apiKeyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", merchantToken);
        var apiKeyResponse = await client.SendAsync(apiKeyRequest);
        Assert.Equal(HttpStatusCode.Created, apiKeyResponse.StatusCode);

        var apiKeyJson = await apiKeyResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("sd_live_", apiKeyJson.GetProperty("plainTextKey").GetString());

        // 4. Merchant creates OAuth client credentials
        var oauthClientRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/merchants/oauth-clients")
        {
            Content = JsonContent.Create(new { name = "Shopify Store App" })
        };
        oauthClientRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", merchantToken);
        var oauthClientResponse = await client.SendAsync(oauthClientRequest);
        Assert.Equal(HttpStatusCode.Created, oauthClientResponse.StatusCode);

        var oauthJson = await oauthClientResponse.Content.ReadFromJsonAsync<JsonElement>();
        var clientId = oauthJson.GetProperty("clientId").GetString();
        var clientSecret = oauthJson.GetProperty("plainTextClientSecret").GetString();

        // 5. Exchange credentials for standard OAuth access token
        var tokenResponse = await client.PostAsJsonAsync("/api/v1/oauth/token", new
        {
            grant_type = "client_credentials",
            client_id = clientId,
            client_secret = clientSecret
        });
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Bearer", tokenJson.GetProperty("token_type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(tokenJson.GetProperty("access_token").GetString()));
    }
}
