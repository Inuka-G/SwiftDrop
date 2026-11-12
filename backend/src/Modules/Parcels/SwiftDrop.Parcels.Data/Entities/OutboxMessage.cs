using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Parcels.Data.Entities;

/// Transactional outbox: an integration event saved in the same transaction as the
/// parcel change, then sent to the broker by OutboxProcessor. A status change is
/// therefore never saved without its event, and never published without being saved.
public class OutboxMessage : BaseEntity
{
    public const int MaxErrorLength = 1000;

    private OutboxMessage() { } // Required by EF Core

    public OutboxMessage(Guid eventId, string routingKey, string payload, DateTime occurredAtUtc)
    {
        Id = eventId;
        RoutingKey = routingKey;
        Payload = payload;
        CreatedAtUtc = occurredAtUtc;
    }

    public string RoutingKey { get; private set; } = default!;
    public string Payload { get; private set; } = default!;
    public DateTime? ProcessedAtUtc { get; private set; }
    public int Attempts { get; private set; }
    public string? LastError { get; private set; }

    public void MarkProcessed(DateTime nowUtc)
    {
        ProcessedAtUtc = nowUtc;
        LastError = null;
    }

    public void MarkFailed(string error)
    {
        Attempts++;
        LastError = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
    }
}
