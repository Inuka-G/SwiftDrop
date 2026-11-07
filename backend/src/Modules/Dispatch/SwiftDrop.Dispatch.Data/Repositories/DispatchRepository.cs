using Microsoft.EntityFrameworkCore;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;

namespace SwiftDrop.Dispatch.Data.Repositories;

internal sealed class DispatchRepository(DispatchDbContext db) : IDispatchRepository
{
    public async Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListUnassignedAsync(Guid? zoneId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Assignments.AsNoTracking().Where(x => x.State == AssignmentStates.Unassigned);
        if (zoneId.HasValue) query = query.Where(x => x.ZoneId == zoneId.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ParcelId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<ParcelAssignment?> GetAsync(Guid parcelId, CancellationToken ct) =>
        db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == parcelId, ct);

    public Task SaveAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task AddFromEventAsync(Guid eventId, ParcelAssignment assignment, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow });
        if (!await db.Assignments.AnyAsync(x => x.ParcelId == assignment.ParcelId, ct)) db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
