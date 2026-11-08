using SwiftDrop.Parcels.Data.Entities;

namespace SwiftDrop.Parcels.Data.Repositories;

public interface IParcelRepository
{
    Task AddAsync(Parcel parcel, CancellationToken ct);
    Task<Parcel?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Parcel?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct);
    Task<IReadOnlyList<Parcel>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}