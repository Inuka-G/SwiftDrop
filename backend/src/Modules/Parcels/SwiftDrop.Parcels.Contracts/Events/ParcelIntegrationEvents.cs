using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.Contracts.Events;

/// Published when a parcel is booked (Dispatch lists it as unassigned, Reporting counts it).
/// Like every Parcels event it carries ids only, never addresses, phone numbers or notes:
/// consumers that need contact details ask Identity.
[RoutingKey("parcel.created")]
public sealed record ParcelCreated(
    Guid ParcelId,
    string TrackingNumber,
    Guid CustomerId,
    Guid ZoneId) : IntegrationEvent;
