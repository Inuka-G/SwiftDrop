using SwiftDrop.AIAssistant.Business.Tools;
using SwiftDrop.AIAssistant.Contracts;

namespace SwiftDrop.AIAssistant.Business.Services;

public sealed class AiAssistantService(
    ILlmClient llmClient,
    IEnumerable<IAssistantTool> tools) : IAiAssistantService
{
    private const int MaximumMessageLength = 1_000;
    private readonly IReadOnlyList<IAssistantTool> _tools = tools.ToList();

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

        var tool = _tools.FirstOrDefault(candidate => candidate.CanHandle(message));
        if (tool is not null)
        {
            var toolAnswer = await tool.ExecuteAsync(message, cancellationToken);
            return new ChatResponse(toolAnswer, [tool.Name]);
        }

        var answer = await llmClient.CompleteAsync(message, cancellationToken);
        return new ChatResponse(answer, []);
    }
}

