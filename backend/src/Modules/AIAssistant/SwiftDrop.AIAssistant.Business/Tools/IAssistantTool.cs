namespace SwiftDrop.AIAssistant.Business.Tools;

public interface IAssistantTool
{
    string Name { get; }
    string Description { get; }
    bool CanHandle(string message);
    Task<string> ExecuteAsync(string message, CancellationToken cancellationToken = default);
}

