using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Contracts.Events;

[RoutingKey("parcel.status.changed")]
public sealed record ParcelStatusChanged(
    Guid ParcelId,
    string TrackingNumber,
    string OldStatus,
    string NewStatus,
    string? Note,
    Guid? CustomerId,
    Guid? MerchantId,
    Guid? CourierId) : IntegrationEvent;
