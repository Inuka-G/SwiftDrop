using Microsoft.EntityFrameworkCore;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.SharedKernel.Authorization;

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

    public Task<CourierProfile?> GetCourierProfileAsync(Guid courierId, CancellationToken ct) =>
        db.CourierProfiles.SingleOrDefaultAsync(x => x.CourierId == courierId, ct);

    public async Task AddCourierProfileAsync(CourierProfile profile, CancellationToken ct) =>
        await db.CourierProfiles.AddAsync(profile, ct);

    public async Task<(IReadOnlyList<CourierProfile> Items, int Total)> ListCouriersAsync(Guid? zoneId, bool? isAvailable,
        int page, int pageSize, CancellationToken ct)
    {
        var query = db.CourierProfiles.AsNoTracking();
        if (zoneId.HasValue) query = query.Where(x => x.ZoneId == zoneId.Value);
        if (isAvailable.HasValue) query = query.Where(x => x.IsAvailable == isAvailable.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.CourierId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetWorkloadsAsync(IReadOnlyCollection<Guid> courierIds, CancellationToken ct)
    {
        if (courierIds.Count == 0) return new Dictionary<Guid, int>();
        return await db.Assignments.AsNoTracking().Where(x => x.CourierId.HasValue && courierIds.Contains(x.CourierId.Value)
                && x.State == AssignmentStates.Assigned)
            .GroupBy(x => x.CourierId!.Value).Select(g => new { CourierId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CourierId, x => x.Count, ct);
    }

    public async Task<(int Total, int Available, int Unavailable, int ActiveDeliveries)> GetCourierSummaryAsync(CancellationToken ct)
    {
        var total = await db.CourierProfiles.CountAsync(ct);
        var available = await db.CourierProfiles.CountAsync(x => x.IsAvailable, ct);
        var active = await db.Assignments.CountAsync(x => x.State == AssignmentStates.Assigned, ct);
        return (total, available, total - available, active);
    }

    public async Task<(int Unassigned, int AvailableCouriers, int CouriersWithActiveDeliveries, int ActiveDeliveries, int TotalCouriers)> GetDashboardSummaryAsync(CancellationToken ct)
    {
        var unassigned = await db.Assignments.CountAsync(x => x.State == AssignmentStates.Unassigned, ct);
        var availableCouriers = await db.CourierProfiles.CountAsync(x => x.IsAvailable, ct);
        var totalCouriers = await db.CourierProfiles.CountAsync(ct);
        var activeQuery = db.Assignments.Where(x => x.State == AssignmentStates.Assigned);
        var activeDeliveries = await activeQuery.CountAsync(ct);
        var couriersWithActiveDeliveries = await activeQuery.Where(x => x.CourierId.HasValue)
            .Select(x => x.CourierId!.Value).Distinct().CountAsync(ct);
        return (unassigned, availableCouriers, couriersWithActiveDeliveries, activeDeliveries, totalCouriers);
    }

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

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

    public async Task ApplyParcelStatusChangedAsync(Guid eventId, Guid parcelId, string status, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == parcelId, ct)
            ?? throw new DispatchEventRetryException("Parcel assignment has not arrived yet.");
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow });
        if (status is "DELIVERED" or "FAILED")
        {
            assignment.State = AssignmentStates.Completed;
            assignment.Version++;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyUserRoleChangedAsync(Guid eventId, Guid userId, string role, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;
        var profile = await db.CourierProfiles.SingleOrDefaultAsync(x => x.CourierId == userId, ct);
        if (string.Equals(role, Roles.Courier, StringComparison.OrdinalIgnoreCase))
        {
            profile ??= db.CourierProfiles.Add(new CourierProfile { CourierId = userId, IsAvailable = false }).Entity;
        }
        else if (profile is not null)
        {
            profile.IsAvailable = false;
            profile.Version++;
        }
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyUserStatusChangedAsync(Guid eventId, Guid userId, bool isActive, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;
        var profile = await db.CourierProfiles.SingleOrDefaultAsync(x => x.CourierId == userId, ct);
        if (!isActive && profile is not null)
        {
            profile.IsAvailable = false;
            profile.Version++;
        }
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}

internal sealed class DispatchEventRetryException(string message) : Exception(message);
