namespace SwiftDrop.Parcels.Contracts.Dtos;

public sealed record ParcelSummaryDto(
    Guid Id,
    string TrackingNumber,
    string RecipientName,
    string DropoffAddress,
    decimal WeightKg,
    decimal EstimatedFee,
    string CurrentStatus,
    DateTime CreatedAtUtc
);