namespace SwiftDrop.Parcels.Contracts.Dtos;

public sealed record ParcelCreatedResponse(
    Guid ParcelId,
    string TrackingNumber,
    decimal EstimatedFee,
    string Status,
    DateTime CreatedAtUtc
);