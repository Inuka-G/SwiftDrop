using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using SwiftDrop.IntegrationTests.Auth;

namespace SwiftDrop.IntegrationTests.Identity;

[Collection(ApiCollection.Name)]
public sealed class UserManagementTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();
    private const string UsersPath = "/api/v1/users";

    [Fact]
    public async Task Swagger_documents_user_management_routes()
    {
        var document = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/users", document);
        Assert.Contains("/api/v1/users/{id}/status", document);
        Assert.Contains("/api/v1/users/{id}/role", document);
    }

    [Fact]
    public async Task Admin_can_deactivate_user_and_find_literal_wildcard_without_secrets()
    {
        var token = await client.LoginAsync("admin");
        var id = await CreateCustomerAsync("Percent%_ Match", "percent-match@example.test");

        var changed = await client.PatchAsJsonAsync($"{UsersPath}/{id}/status", new { isActive = false }, token);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);

        var listed = await client.GetWithTokenAsync($"{UsersPath}?search={Uri.EscapeDataString("percent%_")}&role=CUSTOMER&isActive=false&page=1&pageSize=10", token);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        var body = await listed.Content.ReadFromJsonAsync<JsonElement>();
        var user = Assert.Single(body.GetProperty("items").EnumerateArray());
        Assert.Equal(id, user.GetProperty("id").GetGuid());
        Assert.False(user.GetProperty("isActive").GetBoolean());
        Assert.Equal("LOCAL", user.GetProperty("authProvider").GetString());
        var json = user.GetRawText();
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerUserId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneNumber", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_can_change_a_user_role_and_filter_by_new_role()
    {
        var token = await client.LoginAsync("admin");
        var id = await CreateCustomerAsync("Role Change", "role-change@example.test");

        var changed = await client.PatchAsJsonAsync($"{UsersPath}/{id}/role", new { role = "courier" }, token);

        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var listed = await client.GetWithTokenAsync($"{UsersPath}?role=COURIER&search=ROLE%20CHANGE", token);
        Assert.Equal(HttpStatusCode.OK, listed.StatusCode);
        var body = await listed.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(body.GetProperty("items").EnumerateArray(), user => user.GetProperty("id").GetGuid() == id && user.GetProperty("role").GetString() == "COURIER");
    }

    [Theory]
    [InlineData("/api/v1/users")]
    [InlineData("/api/v1/users/00000000-0000-0000-0000-000000000001/status")]
    [InlineData("/api/v1/users/00000000-0000-0000-0000-000000000001/role")]
    public async Task No_login_gets_401_on_each_user_management_route(string path)
    {
        var response = path.EndsWith("status", StringComparison.Ordinal)
            ? await client.PatchAsJsonAsync(path, new { isActive = true })
            : path.EndsWith("role", StringComparison.Ordinal)
                ? await client.PatchAsJsonAsync(path, new { role = "COURIER" })
                : await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("customer1")]
    [InlineData("merchant1")]
    [InlineData("courier1")]
    public async Task Non_admin_roles_get_403_on_all_routes(string username)
    {
        var token = await client.LoginAsync(username);
        foreach (var (path, method) in Routes())
        {
            var response = method == "GET"
                ? await client.GetWithTokenAsync(path, token)
                : path.EndsWith("status", StringComparison.Ordinal)
                    ? await client.PatchAsJsonAsync(path, new { isActive = true }, token)
                    : await client.PatchAsJsonAsync(path, new { role = "COURIER" }, token);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Dispatcher_can_list_and_change_status_but_cannot_change_roles()
    {
        var dispatcher = CreateRoleToken("DISPATCHER");
        var list = await client.GetWithTokenAsync(UsersPath, dispatcher);
        var status = await client.PatchAsJsonAsync($"{UsersPath}/{Guid.NewGuid()}/status", new { isActive = false }, dispatcher);
        var role = await client.PatchAsJsonAsync($"{UsersPath}/{Guid.NewGuid()}/role", new { role = "COURIER" }, dispatcher);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, role.StatusCode);
    }

    private async Task<Guid> CreateCustomerAsync(string fullName, string email)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            fullName,
            email,
            password = "SecurePass123!",
            phoneNumber = "+15555550123"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("user").GetProperty("id").GetGuid();
    }

    private static IEnumerable<(string Path, string Method)> Routes()
    {
        yield return (UsersPath, "GET");
        yield return ($"{UsersPath}/00000000-0000-0000-0000-000000000001/status", "PATCH");
        yield return ($"{UsersPath}/00000000-0000-0000-0000-000000000001/role", "PATCH");
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

internal static class UserManagementHttpExtensions
{
    public static Task<HttpResponseMessage> PatchAsJsonAsync(this HttpClient client, string path, object body, string? token = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, path) { Content = JsonContent.Create(body) };
        if (token is not null) request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client.SendAsync(request);
    }
}
