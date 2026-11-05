namespace SwiftDrop.Dispatch.Contracts;

// Adapter shape until Parcels publishes its integration-event contract.
public sealed record ParcelCreatedMessage(Guid EventId, Guid ParcelId, string TrackingNumber,
    Guid ZoneId, DateTimeOffset CreatedAt);
