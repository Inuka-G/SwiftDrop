using Microsoft.EntityFrameworkCore;
using System.Data;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.Parcels.Contracts;

namespace SwiftDrop.Dispatch.Data.Repositories;

internal sealed class DispatchRepository(DispatchDbContext db) : IDispatchRepository
{
    public async Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListUnassignedAsync(Guid? zoneId,
        DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
    {
        var query = db.Assignments.AsNoTracking().Where(x =>
            x.State == AssignmentStates.Unassigned && x.CourierId == null);
        if (zoneId.HasValue) query = query.Where(x => x.ZoneId == zoneId.Value);
        if (from.HasValue) query = query.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAt <= to.Value);
        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ParcelId)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task ReconcileParcelsAsync(IReadOnlyCollection<DispatchableParcelSnapshot> parcels, CancellationToken ct)
    {
        foreach (var parcel in parcels)
        {
            var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == parcel.ParcelId, ct);
            if (assignment is null)
            {
                db.Assignments.Add(new ParcelAssignment
                {
                    ParcelId = parcel.ParcelId,
                    TrackingNumber = parcel.TrackingNumber,
                    ZoneId = parcel.ZoneId,
                    CreatedAt = parcel.CreatedAtUtc,
                    State = ParcelDeliveryStatuses.IsTerminal(parcel.CurrentStatus)
                        ? AssignmentStates.Completed
                        : parcel.CourierId.HasValue ? AssignmentStates.Assigned : AssignmentStates.Unassigned,
                    CurrentStatus = parcel.CurrentStatus,
                    CourierId = parcel.CourierId,
                    AssignedAt = parcel.AssignedAtUtc,
                    WeightKg = parcel.WeightKg,
                    SizeCategory = parcel.SizeCategory,
                    StatusOccurredAtUtc = (parcel.AssignedAtUtc ?? parcel.CreatedAtUtc).ToUnixTimeMilliseconds(),
                    Version = 0
                });
                continue;
            }

            // Parcels is the assignment and lifecycle source of truth. Reconcile
            // stale event-fed rows to the current Parcels state without regressing
            // a newer Dispatch status event.
            var changed = assignment.TrackingNumber != parcel.TrackingNumber ||
                assignment.ZoneId != parcel.ZoneId || assignment.CreatedAt != parcel.CreatedAtUtc ||
                assignment.CourierId != parcel.CourierId || assignment.AssignedAt != parcel.AssignedAtUtc ||
                assignment.WeightKg != parcel.WeightKg || assignment.SizeCategory != parcel.SizeCategory;
            assignment.TrackingNumber = parcel.TrackingNumber;
            assignment.ZoneId = parcel.ZoneId;
            assignment.CreatedAt = parcel.CreatedAtUtc;
            assignment.CourierId = parcel.CourierId;
            assignment.AssignedAt = parcel.AssignedAtUtc;
            assignment.WeightKg = parcel.WeightKg;
            assignment.SizeCategory = parcel.SizeCategory;
            if (string.Equals(assignment.CurrentStatus, "Created", StringComparison.OrdinalIgnoreCase) ||
                assignment.StatusOccurredAtUtc <= (parcel.AssignedAtUtc ?? parcel.CreatedAtUtc).ToUnixTimeMilliseconds())
            {
                var reconciledStatus = parcel.CourierId.HasValue &&
                    string.Equals(parcel.CurrentStatus, "Created", StringComparison.OrdinalIgnoreCase)
                    ? "Assigned" : parcel.CurrentStatus;
                var statusOccurredAt = Math.Max(assignment.StatusOccurredAtUtc,
                    (parcel.AssignedAtUtc ?? parcel.CreatedAtUtc).ToUnixTimeMilliseconds());
                changed |= assignment.CurrentStatus != reconciledStatus || assignment.StatusOccurredAtUtc != statusOccurredAt;
                assignment.CurrentStatus = reconciledStatus;
                assignment.StatusOccurredAtUtc = statusOccurredAt;
            }
            var reconciledState = ParcelDeliveryStatuses.IsTerminal(parcel.CurrentStatus)
                ? AssignmentStates.Completed
                : parcel.CourierId.HasValue ? AssignmentStates.Assigned
                : AssignmentStates.Unassigned;
            changed |= assignment.State != reconciledState;
            assignment.State = reconciledState;
            if (changed) assignment.Version++;
        }

        var currentParcelIds = parcels.Select(parcel => parcel.ParcelId).ToArray();
        var staleUnassigned = await db.Assignments
            .Where(assignment => assignment.State == AssignmentStates.Unassigned &&
                !currentParcelIds.Contains(assignment.ParcelId))
            .ToListAsync(ct);
        foreach (var stale in staleUnassigned)
        {
            stale.State = AssignmentStates.Assigned;
            stale.CurrentStatus = string.Equals(stale.CurrentStatus, "Created", StringComparison.OrdinalIgnoreCase)
                ? "Assigned" : stale.CurrentStatus;
            stale.Version++;
        }

        await db.SaveChangesAsync(ct);
    }

    public async Task<(IReadOnlyList<ParcelAssignment> Items, int Total)> ListParcelsAsync(string? status,
        string? trackingNumber, int page, int pageSize, string? assignmentState, CancellationToken ct)
    {
        var query = db.Assignments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(x => x.CurrentStatus.ToLower() == normalizedStatus);
        }
        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            var normalizedTracking = trackingNumber.Trim().ToLower();
            query = query.Where(x => x.TrackingNumber.ToLower().Contains(normalizedTracking));
        }
        if (!string.IsNullOrWhiteSpace(assignmentState))
        {
            var normalizedAssignmentState = assignmentState.Trim().ToUpperInvariant();
            query = query.Where(x => x.State.ToUpper() == normalizedAssignmentState);
        }
        var all = await query.OrderByDescending(x => x.CreatedAt).ThenBy(x => x.ParcelId).ToListAsync(ct);
        // Parcels guarantees tracking-number uniqueness. Collapse duplicates that
        // may exist in older Dispatch projections while preserving the newest row.
        var unique = all.GroupBy(x => x.TrackingNumber, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First()).ToList();
        var total = unique.Count;
        var items = unique.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (items, total);
    }

    public Task<ParcelAssignment?> GetAsync(Guid parcelId, CancellationToken ct) =>
        db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == parcelId, ct);

    public Task<CourierProfile?> GetCourierProfileAsync(Guid courierId, CancellationToken ct) =>
        db.CourierProfiles.SingleOrDefaultAsync(x => x.CourierId == courierId, ct);

    public async Task AddCourierProfileAsync(CourierProfile profile, CancellationToken ct) =>
        await db.CourierProfiles.AddAsync(profile, ct);

    public async Task<CourierProfileDeleteStatus> DeleteCourierProfileAsync(Guid courierId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var profile = await db.CourierProfiles.SingleOrDefaultAsync(x => x.CourierId == courierId, ct);
        if (profile is null) return CourierProfileDeleteStatus.NotFound;

        var hasActiveDeliveries = await db.Assignments.AnyAsync(x =>
            x.CourierId == courierId && x.State == AssignmentStates.Assigned, ct);
        if (hasActiveDeliveries) return CourierProfileDeleteStatus.HasActiveDeliveries;

        db.CourierProfiles.Remove(profile);
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return CourierProfileDeleteStatus.Deleted;
        }
        catch (DbUpdateConcurrencyException)
        {
            return CourierProfileDeleteStatus.ConcurrencyConflict;
        }
    }

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

    public async Task EnqueueOutboxEventAsync(Guid eventId, string routingKey, string payload,
        DateTimeOffset occurredAt, CancellationToken ct)
    {
        db.OutboxMessages.Add(new DispatchOutboxMessage
        {
            EventId = eventId, RoutingKey = routingKey, Payload = payload,
            CreatedAt = occurredAt.ToUnixTimeMilliseconds()
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DispatchOutboxRecord>> GetPendingOutboxAsync(
        int limit, CancellationToken ct) => await db.OutboxMessages.AsNoTracking()
        .Where(x => x.ProcessedAt == null).OrderBy(x => x.CreatedAt).Take(limit)
        .Select(x => new DispatchOutboxRecord(x.EventId, x.RoutingKey, x.Payload)).ToListAsync(ct);

    public async Task MarkOutboxProcessedAsync(Guid eventId, CancellationToken ct)
    {
        var item = await db.OutboxMessages.SingleOrDefaultAsync(x => x.EventId == eventId, ct);
        if (item is null) return;
        item.ProcessedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        await db.SaveChangesAsync(ct);
    }

    public async Task RecordOutboxFailureAsync(Guid eventId, CancellationToken ct)
    {
        var item = await db.OutboxMessages.SingleOrDefaultAsync(x => x.EventId == eventId, ct);
        if (item is null) return;
        item.Attempts++;
        await db.SaveChangesAsync(ct);
    }

    public async Task AddFromEventAsync(Guid eventId, ParcelAssignment assignment, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow });
        var existing = await db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == assignment.ParcelId, ct);
        if (existing is null)
        {
            db.Assignments.Add(assignment);
        }
        else
        {
            var wasTerminal = ParcelDeliveryStatuses.IsTerminal(existing.CurrentStatus);
            existing.TrackingNumber = assignment.TrackingNumber;
            if (existing.ZoneId == Guid.Empty) existing.ZoneId = assignment.ZoneId;
            existing.CreatedAt = assignment.CreatedAt;
            if (!wasTerminal && assignment.CreatedAt.ToUnixTimeMilliseconds() >= existing.StatusOccurredAtUtc)
            {
                existing.CurrentStatus = "Created";
                existing.StatusOccurredAtUtc = assignment.CreatedAt.ToUnixTimeMilliseconds();
                if (existing.CourierId is null)
                    existing.State = AssignmentStates.Unassigned;
            }
            existing.Version++;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyCourierAssignedAsync(Guid eventId, Guid parcelId, string trackingNumber,
        Guid courierId, DateTimeOffset assignedAtUtc, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == eventId, ct)) return;
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == parcelId, ct);
        if (assignment is null)
        {
            assignment = new ParcelAssignment
            {
                ParcelId = parcelId,
                TrackingNumber = trackingNumber,
                State = AssignmentStates.Assigned,
                CourierId = courierId,
                AssignedAt = assignedAtUtc,
                CurrentStatus = "Created",
                StatusOccurredAtUtc = assignedAtUtc.ToUnixTimeMilliseconds(),
                CreatedAt = assignedAtUtc,
                Version = 0
            };
            db.Assignments.Add(assignment);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(assignment.TrackingNumber)) assignment.TrackingNumber = trackingNumber;
            if (assignment.CourierId is null) assignment.CourierId = courierId;
            if (assignment.AssignedAt is null) assignment.AssignedAt = assignedAtUtc;
            if (!ParcelDeliveryStatuses.IsTerminal(assignment.CurrentStatus))
                assignment.State = AssignmentStates.Assigned;
            if (string.Equals(assignment.CurrentStatus, "Created", StringComparison.OrdinalIgnoreCase))
                assignment.CurrentStatus = "Assigned";
            assignment.Version++;
        }
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = eventId, ProcessedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task RecordCourierAssignmentAsync(Guid parcelId, Guid courierId, Guid assignedBy,
        DateTimeOffset assignedAtUtc, CancellationToken ct)
    {
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == parcelId, ct);
        if (assignment is null)
            throw new InvalidOperationException("The dispatch projection for this parcel is missing.");

        // Parcels is authoritative for whether the assignment succeeds. Once it
        // does, update the local board immediately instead of waiting for RabbitMQ.
        if (assignment.State != AssignmentStates.Unassigned)
            throw new InvalidOperationException("The parcel is no longer unassigned in Dispatch.");

        assignment.CourierId = courierId;
        assignment.AssignedBy = assignedBy;
        assignment.AssignedAt = assignedAtUtc;
        assignment.State = AssignmentStates.Assigned;
        if (string.Equals(assignment.CurrentStatus, "Created", StringComparison.OrdinalIgnoreCase))
            assignment.CurrentStatus = "Assigned";
        assignment.Version++;
        await db.SaveChangesAsync(ct);
    }

    public async Task ApplyParcelStatusChangedAsync(ParcelStatusChangedMessage message, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        if (await db.ProcessedEvents.AnyAsync(x => x.EventId == message.EventId, ct)) return;
        db.ProcessedEvents.Add(new ProcessedEvent { EventId = message.EventId, ProcessedAt = DateTimeOffset.UtcNow });
        var assignment = await db.Assignments.SingleOrDefaultAsync(x => x.ParcelId == message.ParcelId, ct);
        if (assignment is null)
        {
            assignment = new ParcelAssignment
            {
                ParcelId = message.ParcelId,
                TrackingNumber = message.TrackingNumber,
                ZoneId = Guid.Empty,
                CreatedAt = message.OccurredAtUtc,
                State = message.CourierId.HasValue ? AssignmentStates.Assigned : AssignmentStates.Unassigned,
                CourierId = message.CourierId,
                CurrentStatus = message.Status,
                StatusOccurredAtUtc = message.OccurredAtUtc.ToUnixTimeMilliseconds(),
                Version = 0
            };
            if (ParcelDeliveryStatuses.IsTerminal(message.Status)) assignment.State = AssignmentStates.Completed;
            db.Assignments.Add(assignment);
        }
        else if (message.OccurredAtUtc.ToUnixTimeMilliseconds() >= assignment.StatusOccurredAtUtc &&
                 !(ParcelDeliveryStatuses.IsTerminal(assignment.CurrentStatus) &&
                   !ParcelDeliveryStatuses.IsTerminal(message.Status)))
        {
            assignment.TrackingNumber = string.IsNullOrWhiteSpace(assignment.TrackingNumber)
                ? message.TrackingNumber : assignment.TrackingNumber;
            assignment.CurrentStatus = message.Status;
            assignment.StatusOccurredAtUtc = message.OccurredAtUtc.ToUnixTimeMilliseconds();
            if (assignment.CourierId is null && message.CourierId.HasValue)
            {
                assignment.CourierId = message.CourierId;
                if (assignment.State == AssignmentStates.Unassigned)
                    assignment.State = AssignmentStates.Assigned;
            }
            if (ParcelDeliveryStatuses.IsTerminal(message.Status)) assignment.State = AssignmentStates.Completed;
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
