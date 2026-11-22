namespace SwiftDrop.SharedKernel.Messaging;

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string Exchange { get; set; } = "swiftdrop.events";
    public string DeadLetterExchange => $"{Exchange}.dlx";
    public int MaxHandlerAttempts { get; set; } = 3;
    public string[] RequiredOutboxRoutes { get; set; } = [];
}
