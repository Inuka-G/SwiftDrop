using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Contracts;
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

    public async Task<IReadOnlyList<Parcel>> GetForCourierAsync(Guid courierId, DateTime closedSinceUtc, CancellationToken ct) =>
        await db.Parcels
            .AsNoTracking()
            .Where(p => p.CourierId == courierId
                && ((p.CurrentStatus != ParcelStatuses.Delivered && p.CurrentStatus != ParcelStatuses.Failed)
                    || p.UpdatedAtUtc >= closedSinceUtc))
            .OrderBy(p => p.AssignedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ParcelStatusHistory>> GetHistoryAsync(Guid parcelId, CancellationToken ct) =>
        await db.StatusHistory
            .AsNoTracking()
            .Where(h => h.ParcelId == parcelId)
            .OrderBy(h => h.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<DeliveryProof?> GetProofAsync(Guid parcelId, CancellationToken ct) =>
        db.DeliveryProofs.AsNoTracking().FirstOrDefaultAsync(p => p.ParcelId == parcelId, ct);

    public Task<FailureReport?> GetLatestFailureAsync(Guid parcelId, CancellationToken ct) =>
        db.FailureReports
            .AsNoTracking()
            .Where(f => f.ParcelId == parcelId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    public void AddHistory(ParcelStatusHistory entry) => db.StatusHistory.Add(entry);
    public void AddProof(DeliveryProof proof) => db.DeliveryProofs.Add(proof);
    public void AddFailureReport(FailureReport report) => db.FailureReports.Add(report);
    public void AddOutboxMessage(OutboxMessage message) => db.OutboxMessages.Add(message);

    public Task SaveChangesAsync(CancellationToken ct) =>
        db.SaveChangesAsync(ct);
}
