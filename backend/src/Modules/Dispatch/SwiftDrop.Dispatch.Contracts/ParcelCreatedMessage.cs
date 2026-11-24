namespace SwiftDrop.Dispatch.Contracts;

// Dispatch-local shape populated from Parcels.Contracts integration events.
public sealed record ParcelCreatedMessage(Guid EventId, Guid ParcelId, string TrackingNumber,
    Guid ZoneId, DateTimeOffset CreatedAt);
