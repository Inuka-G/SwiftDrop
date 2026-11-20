namespace SwiftDrop.AIAssistant.Api.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Endpoint { get; init; } =
        "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions";
    public string Model { get; init; } = "gemini-3.5-flash-lite";
    public string? ApiKey { get; init; }
    public string InternalApiBaseUrl { get; init; } = "http://localhost:5017/api/v1/";
}

