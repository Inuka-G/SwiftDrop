namespace SwiftDrop.Dispatch.Data.Entities;

internal sealed class DispatchOutboxMessage
{
    public Guid EventId { get; set; }
    public string RoutingKey { get; set; } = "";
    public string Payload { get; set; } = "";
    public long CreatedAt { get; set; }
    public long? ProcessedAt { get; set; }
    public int Attempts { get; set; }
}
