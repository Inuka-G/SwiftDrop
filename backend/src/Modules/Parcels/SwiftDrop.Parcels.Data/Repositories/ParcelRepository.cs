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

    public Task<Parcel?> GetMerchantOrderAsync(Guid merchantId, Guid parcelId, CancellationToken ct) =>
        db.Parcels.AsNoTracking().SingleOrDefaultAsync(p => p.MerchantId == merchantId && p.Id == parcelId, ct);

    public async Task<(IReadOnlyList<Parcel> Items, int TotalCount)> ListMerchantOrdersAsync(
        Guid merchantId, string? status, int offset, int limit, CancellationToken ct)
    {
        var query = db.Parcels.AsNoTracking().Where(p => p.MerchantId == merchantId);
        if (status is not null)
            query = query.Where(p => p.CurrentStatus == status);

        var count = await query.CountAsync(ct);
        var items = await query.OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
            .Skip(offset).Take(limit).ToListAsync(ct);
        return (items, count);
    }

    public async Task<IReadOnlyList<Parcel>> GetByCustomerIdAsync(Guid customerId, CancellationToken ct) =>
        await db.Parcels
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<DispatchableParcelSnapshot>> GetCreatedUnassignedAsync(CancellationToken ct) =>
        await db.Parcels.AsNoTracking()
            .Where(p => p.CurrentStatus == ParcelStatuses.Created && p.CourierId == null)
            .OrderBy(p => p.CreatedAtUtc)
            .Select(p => new DispatchableParcelSnapshot(p.Id, p.TrackingNumber, p.ZoneId,
                new DateTimeOffset(p.CreatedAtUtc)))
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
