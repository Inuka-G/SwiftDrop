namespace SwiftDrop.Parcels.Contracts.Dtos;

public sealed record PublicTrackingDto(
    string TrackingNumber,
    string CurrentStatus,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    string DestinationArea
);