using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Contracts.Events;

/// Published (through the Parcels outbox) every time a parcel's status changes, including
/// "None" -> Created at booking. Consumed by Notifications, Realtime, Webhooks and Reporting.
/// FailureReason is one of FailureReasons when NewStatus is Failed.
[RoutingKey("parcel.status.changed")]
public sealed record ParcelStatusChanged(
    Guid ParcelId,
    string TrackingNumber,
    string OldStatus,
    string NewStatus,
    string? Note,
    Guid? CustomerId,
    Guid? MerchantId,
    Guid? CourierId,
    string? FailureReason = null) : IntegrationEvent;
