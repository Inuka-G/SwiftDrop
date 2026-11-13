using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.IntegrationTests.Auth;
using SwiftDrop.Reporting.Business.Consumers;

namespace SwiftDrop.IntegrationTests.Reporting;

[Collection(ApiCollection.Name)]
public sealed class ReportingWorkflowTests(SwiftDropApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task OpenApi_lists_reporting_routes()
    {
        var document = await client.GetStringAsync("/swagger/v1/swagger.json");
        Assert.Contains("/api/v1/reports/deliveries-per-day", document);
        Assert.Contains("/api/v1/reports/success-failure-rate", document);
        Assert.Contains("/api/v1/reports/summary", document);
        Assert.Contains("/api/v1/dispatch/summary", document);
    }

    [Fact]
    public async Task Events_are_projected_to_dispatcher_reports()
    {
        var date = new DateTimeOffset(2020, 1, 2, 10, 0, 0, TimeSpan.Zero);
        using (var scope = factory.Services.CreateScope())
        {
            var created = scope.ServiceProvider.GetRequiredService<ParcelCreatedConsumer>();
            var status = scope.ServiceProvider.GetRequiredService<ParcelStatusChangedConsumer>();
            await created.HandleAsync(new(Guid.NewGuid(), date), default);
            await status.HandleAsync(new(Guid.NewGuid(), date, "DELIVERED"), default);
            await status.HandleAsync(new(Guid.NewGuid(), date, "FAILED"), default);
        }

        var token = await client.LoginAsync("admin");
        var response = await client.GetWithTokenAsync("/api/v1/reports/deliveries-per-day?from=2020-01-02&to=2020-01-02", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
        var row = body[0];
        Assert.Equal("2020-01-02", row.GetProperty("date").GetString());
        Assert.Equal(1, row.GetProperty("created").GetInt32());
        Assert.Equal(1, row.GetProperty("delivered").GetInt32());
        Assert.Equal(1, row.GetProperty("failed").GetInt32());

        var summaryResponse = await client.GetWithTokenAsync("/api/v1/reports/summary", token);
        var summary = await summaryResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("date,created,delivered,failed,successRate", string.Join(',', summary.EnumerateObject().Select(x => x.Name)));
        Assert.True(DateOnly.TryParse(summary.GetProperty("date").GetString(), out _));

        var rateResponse = await client.GetWithTokenAsync("/api/v1/reports/success-failure-rate?from=2020-01-02&to=2020-01-02", token);
        Assert.Equal(HttpStatusCode.OK, rateResponse.StatusCode);
        var rate = await rateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0.5m, rate.GetProperty("successRate").GetDecimal());
    }

    [Fact]
    public async Task Invalid_and_oversized_ranges_return_400()
    {
        var token = await client.LoginAsync("admin");
        var reversed = await client.GetWithTokenAsync("/api/v1/reports/deliveries-per-day?from=2026-03-02&to=2026-03-01", token);
        var oversized = await client.GetWithTokenAsync("/api/v1/reports/deliveries-per-day?from=2025-01-01&to=2026-01-02", token);
        Assert.Equal(HttpStatusCode.BadRequest, reversed.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, oversized.StatusCode);
    }
}
