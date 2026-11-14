namespace SwiftDrop.AIAssistant.Contracts;

public sealed record ChatRequest(string Message);

public sealed record ChatResponse(
    string Message,
    IReadOnlyList<string> ToolsUsed);
