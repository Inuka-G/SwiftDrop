using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using SwiftDrop.AIAssistant.Business.Tools;

namespace SwiftDrop.AIAssistant.Api.Tools;

internal sealed partial class EstimateFeeTool(HttpClient httpClient) : IAssistantTool
{
    public string Name => "EstimateFee";
    public string Description => "Estimates a standard delivery fee from a zone name and parcel weight.";

    public bool CanHandle(string message) =>
        message.Contains("fee", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("how much", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("price", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var weightMatch = WeightRegex().Match(message);
        if (!weightMatch.Success ||
            !decimal.TryParse(weightMatch.Groups[1].Value, NumberStyles.Number,
                CultureInfo.InvariantCulture, out var weightKg) ||
            weightKg <= 0)
        {
            return "Please include the parcel weight in kilograms, for example 2 kg.";
        }

        var zones = await httpClient.GetFromJsonAsync<List<ZoneDto>>(
            "zones", cancellationToken) ?? [];
        var zone = zones.FirstOrDefault(candidate =>
            message.Contains(candidate.ZoneName, StringComparison.OrdinalIgnoreCase));

        if (zone is null)
        {
            var names = string.Join(", ", zones.Where(z => z.IsActive).Select(z => z.ZoneName));
            return $"Please include a delivery zone. Available zones: {names}.";
        }

        var url = $"zones/{zone.Id}/estimate?weightKg={weightKg.ToString(CultureInfo.InvariantCulture)}&serviceType=1";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var estimate = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);
        var total = estimate.GetProperty("total").GetDecimal();

        return $"Estimated standard delivery fee for {weightKg:0.##} kg to " +
               $"{zone.ZoneName} is LKR {total:0.00}.";
    }

    private sealed record ZoneDto(Guid Id, string ZoneName, string Description, bool IsActive);

    [GeneratedRegex(@"(\d+(?:\.\d+)?)\s*(?:kg|kilograms?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WeightRegex();
}

