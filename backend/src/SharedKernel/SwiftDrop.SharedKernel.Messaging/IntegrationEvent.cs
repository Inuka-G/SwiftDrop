namespace SwiftDrop.SharedKernel.Messaging;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

[AttributeUsage(AttributeTargets.Class)]
public sealed class RoutingKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}