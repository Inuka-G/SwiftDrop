using SwiftDrop.Parcels.Contracts.Dtos;

namespace SwiftDrop.Parcels.Business.Services;

public interface IParcelService
{
    Task<ParcelCreatedResponse> CreateParcelAsync(Guid customerId, CreateParcelRequest request, CancellationToken ct);
    Task<IReadOnlyList<ParcelSummaryDto>> GetCustomerHistoryAsync(Guid customerId, CancellationToken ct);
    Task<PublicTrackingDto?> GetPublicTrackingAsync(string trackingNumber, CancellationToken ct);
    Task<ParcelDetailsDto?> GetReceiptDetailsAsync(Guid parcelId, Guid customerId, CancellationToken ct);
}

public sealed record ParcelDetailsDto(
    Guid Id,
    string TrackingNumber,
    string PickupAddress,
    string DropoffAddress,
    string RecipientName,
    decimal WeightKg,
    decimal EstimatedFee,
    string CurrentStatus,
    DateTime CreatedAtUtc
);