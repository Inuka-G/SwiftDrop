namespace SwiftDrop.AIAssistant.Business.Services;

public interface ILlmClient
{
    Task<string> CompleteAsync(string message, CancellationToken cancellationToken = default);
}

