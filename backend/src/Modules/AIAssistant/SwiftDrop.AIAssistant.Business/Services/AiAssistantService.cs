using SwiftDrop.AIAssistant.Contracts;

namespace SwiftDrop.AIAssistant.Business.Services;

public sealed class AiAssistantService(
    ILlmClient llmClient,
    IMcpClient mcpClient) : IAiAssistantService
{
    private const int MaximumMessageLength = 1_000;

    public async Task<ChatResponse> ChatAsync(
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("A message is required.", nameof(request));
        }

        if (message.Length > MaximumMessageLength)
        {
            throw new ArgumentException(
                $"The message cannot exceed {MaximumMessageLength} characters.",
                nameof(request));
        }

        var toolName = SelectTool(message);
        if (toolName is not null)
        {
            var toolAnswer = await mcpClient.CallToolAsync(toolName, message, cancellationToken);
            return new ChatResponse(toolAnswer, [toolName]);
        }

        var answer = await llmClient.CompleteAsync(message, cancellationToken);
        return new ChatResponse(answer, []);
    }

    private static string? SelectTool(string message)
    {
        if (message.Contains("track", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("where is my parcel", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("parcel status", StringComparison.OrdinalIgnoreCase))
        {
            return "TrackParcel";
        }

        if (message.Contains("fee", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("cost", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("how much", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("price", StringComparison.OrdinalIgnoreCase))
        {
            return "EstimateFee";
        }

        return null;
    }
}

