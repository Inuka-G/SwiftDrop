
namespace SwiftDrop.Realtime.Business.DTOs;

public sealed record DeliveryStatusUpdate(
    Guid ParcelId,
    string TrackingNumber,
    string Status,
    DateTime StatusChangedAtUtc
);
