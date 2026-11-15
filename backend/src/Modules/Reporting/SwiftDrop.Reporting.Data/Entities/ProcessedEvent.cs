namespace SwiftDrop.Reporting.Data.Entities;

internal sealed class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTimeOffset ProcessedAt { get; set; }
}
