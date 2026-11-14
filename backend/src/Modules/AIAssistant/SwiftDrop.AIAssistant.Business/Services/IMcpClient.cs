namespace SwiftDrop.AIAssistant.Business.Services;

public interface IMcpClient
{
    Task<string> CallToolAsync(
        string toolName,
        string message,
        CancellationToken cancellationToken = default);
}
