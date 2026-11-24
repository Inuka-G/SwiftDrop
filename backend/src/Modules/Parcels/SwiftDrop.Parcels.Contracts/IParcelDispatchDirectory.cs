namespace SwiftDrop.Parcels.Contracts;

/// Minimal Parcels-owned view used by Dispatch to reconcile parcels if a broker
/// consumer queue was unavailable when a ParcelCreated event was published.
public interface IParcelDispatchDirectory
{
    Task<IReadOnlyList<DispatchableParcelSnapshot>> GetCreatedUnassignedAsync(CancellationToken ct);
}

public sealed record DispatchableParcelSnapshot(
    Guid ParcelId,
    string TrackingNumber,
    Guid ZoneId,
    DateTimeOffset CreatedAtUtc);
