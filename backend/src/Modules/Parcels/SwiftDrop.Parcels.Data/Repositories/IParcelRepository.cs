using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Contracts;

namespace SwiftDrop.Parcels.Data.Repositories;

public interface IParcelRepository
{
    Task AddAsync(Parcel parcel, CancellationToken ct);
    Task<Parcel?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Parcel?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct);
    Task<IReadOnlyList<Parcel>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct);
    Task<IReadOnlyList<DispatchableParcelSnapshot>> GetCreatedUnassignedAsync(CancellationToken ct);
    Task<Parcel?> GetMerchantOrderAsync(Guid merchantId, Guid parcelId, CancellationToken ct);
    Task<(IReadOnlyList<Parcel> Items, int TotalCount)> ListMerchantOrdersAsync(
        Guid merchantId, string? status, int offset, int limit, CancellationToken ct);

    /// The courier's open deliveries plus those they closed (Delivered/Failed) since closedSinceUtc.
    Task<IReadOnlyList<Parcel>> GetForCourierAsync(Guid courierId, DateTime closedSinceUtc, CancellationToken ct);

    Task<IReadOnlyList<ParcelStatusHistory>> GetHistoryAsync(Guid parcelId, CancellationToken ct);
    Task<DeliveryProof?> GetProofAsync(Guid parcelId, CancellationToken ct);
    Task<FailureReport?> GetLatestFailureAsync(Guid parcelId, CancellationToken ct);

    // Staged changes are written together by SaveChangesAsync, in one transaction.
    void AddHistory(ParcelStatusHistory entry);
    void AddProof(DeliveryProof proof);
    void AddFailureReport(FailureReport report);
    void AddOutboxMessage(OutboxMessage message);

    Task SaveChangesAsync(CancellationToken ct);
}
