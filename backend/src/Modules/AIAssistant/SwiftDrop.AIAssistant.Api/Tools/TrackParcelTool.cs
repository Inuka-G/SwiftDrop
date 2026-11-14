using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using SwiftDrop.AIAssistant.Business.Tools;

namespace SwiftDrop.AIAssistant.Api.Tools;

internal sealed partial class TrackParcelTool(HttpClient httpClient) : IAssistantTool
{
    public string Name => "TrackParcel";
    public string Description => "Looks up the latest parcel state using a SwiftDrop tracking number.";

    public bool CanHandle(string message) =>
        message.Contains("track", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("where is my parcel", StringComparison.OrdinalIgnoreCase) ||
        message.Contains("parcel status", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(
        string message,
        CancellationToken cancellationToken = default)
    {
        var match = TrackingNumberRegex().Match(message);
        if (!match.Success)
        {
            return "Please include your tracking number, for example SD-1234567.";
        }

        var trackingNumber = match.Value.ToUpperInvariant();
        using var response = await httpClient.GetAsync(
            $"parcels/track/{Uri.EscapeDataString(trackingNumber)}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return $"I could not find a parcel with tracking number {trackingNumber}.";
        }

        response.EnsureSuccessStatusCode();
        var parcel = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: cancellationToken);

        var status = parcel.TryGetProperty("currentStatus", out var statusElement)
            ? statusElement.GetString()
            : "Unknown";

        return $"Parcel {trackingNumber} is currently: {status}.";
    }

    [GeneratedRegex(@"\bSD-[A-Z0-9-]{5,24}\b", RegexOptions.IgnoreCase)]
    private static partial Regex TrackingNumberRegex();
}

