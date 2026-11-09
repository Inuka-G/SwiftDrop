using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Persistence;

namespace SwiftDrop.Parcels.Data.Repositories;

internal sealed class ParcelRepository(ParcelsDbContext db) : IParcelRepository
{
    public async Task AddAsync(Parcel parcel, CancellationToken ct) =>
        await db.Parcels.AddAsync(parcel, ct);

    public Task<Parcel?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Parcels.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Parcel?> GetByTrackingNumberAsync(string trackingNumber, CancellationToken ct) =>
        db.Parcels.AsNoTracking().FirstOrDefaultAsync(p => p.TrackingNumber == trackingNumber, ct);

    public async Task<IReadOnlyList<Parcel>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct) =>
        await db.Parcels
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}