using SwiftDrop.AIAssistant.Contracts;

namespace SwiftDrop.AIAssistant.Business.Services;

public interface IAiAssistantService
{
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken cancellationToken = default);
}

