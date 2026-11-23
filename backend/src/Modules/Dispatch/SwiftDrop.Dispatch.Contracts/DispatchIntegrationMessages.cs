namespace SwiftDrop.Dispatch.Contracts;

// Local adapter message shapes pending the owning modules' Contracts definitions.
public sealed record ParcelStatusChangedMessage(Guid EventId, Guid ParcelId, string TrackingNumber,
    Guid? CourierId, string Status, DateTimeOffset OccurredAtUtc)
{
    public ParcelStatusChangedMessage(Guid eventId, Guid parcelId, string status)
        : this(eventId, parcelId, string.Empty, null, status, DateTimeOffset.UtcNow) { }
}
public sealed record UserRoleChangedMessage(Guid EventId, Guid UserId, string Role);
public sealed record UserStatusChangedMessage(Guid EventId, Guid UserId, bool IsActive);
