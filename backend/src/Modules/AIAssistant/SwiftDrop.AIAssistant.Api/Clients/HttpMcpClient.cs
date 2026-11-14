using System.Net.Http.Json;
using System.Text.Json;
using SwiftDrop.AIAssistant.Business.Services;

namespace SwiftDrop.AIAssistant.Api.Clients;

internal sealed class HttpMcpClient(HttpClient httpClient) : IMcpClient
{
    public async Task<string> CallToolAsync(
        string toolName,
        string message,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString("N"),
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments = new { message }
            }
        };

        using var response = await httpClient.PostAsJsonAsync("", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken),
            cancellationToken: cancellationToken);

        var root = document.RootElement;
        if (root.TryGetProperty("error", out var error))
        {
            var errorMessage = error.TryGetProperty("message", out var errorText)
                ? errorText.GetString()
                : "The MCP server returned an error.";
            throw new InvalidOperationException(errorMessage);
        }

        return root
            .GetProperty("result")
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "The MCP tool returned an empty response.";
    }
}
