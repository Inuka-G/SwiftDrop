namespace SwiftDrop.Parcels.Contracts.Events;

/// Events published by the Parcels module. They carry ids and statuses only, never
/// addresses, phone numbers or notes: consumers that need contact details ask Identity.
public static class ParcelEventRoutingKeys
{
    public const string Created = "parcels.created";
    public const string StatusChanged = "parcels.status-changed";
    public const string CourierAssigned = "parcels.courier-assigned";
}

public sealed record ParcelCreatedIntegrationEvent(
    Guid EventId,
    Guid ParcelId,
    string TrackingNumber,
    Guid? CustomerId,
    Guid ZoneId,
    DateTime OccurredAtUtc,
    Guid? MerchantId = null);

/// Consumed by Notifications (email/SMS), Webhooks (merchant callbacks), Realtime (live
/// tracking), Reporting (daily stats) and Dispatch (courier workload).
public sealed record ParcelStatusChangedIntegrationEvent(
    Guid EventId,
    Guid ParcelId,
    string TrackingNumber,
    Guid? CustomerId,
    Guid? CourierId,
    string PreviousStatus,
    string NewStatus,
    string? FailureReason,
    DateTime OccurredAtUtc,
    Guid? MerchantId = null);

public sealed record CourierAssignedIntegrationEvent(
    Guid EventId,
    Guid ParcelId,
    string TrackingNumber,
    Guid CourierId,
    DateTime AssignedAtUtc);
