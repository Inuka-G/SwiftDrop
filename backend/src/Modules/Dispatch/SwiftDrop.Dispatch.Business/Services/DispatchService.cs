using Microsoft.EntityFrameworkCore;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.Business.Services;

internal sealed class DispatchService(IDispatchRepository repository,
    ICourierEligibilityGateway couriers, IParcelAssignmentGateway parcels,
    IDispatchEventPublisher events, IZoneLookup zones) : IDispatchService
{
    public async Task<UnassignedParcelPage> ListUnassignedAsync(Guid? zoneId, DateTimeOffset? from,
        DateTimeOffset? to, int page, int pageSize, CancellationToken ct)
    {
        var (items, total) = await repository.ListUnassignedAsync(zoneId, from, to, page, pageSize, ct);
        return new(items.Select(x => new UnassignedParcelDto(x.ParcelId, x.TrackingNumber, x.ZoneId, x.CreatedAt)).ToList(),
            page, pageSize, total);
    }

    public async Task<DispatchResult<AssignmentDto>> AssignAsync(Guid parcelId, Guid courierId,
        Guid assignedBy, CancellationToken ct)
    {
        var assignment = await repository.GetAsync(parcelId, ct);
        if (assignment is null) return DispatchResult<AssignmentDto>.Fail(DispatchFailure.ParcelNotFound);
        if (assignment.State != AssignmentStates.Unassigned)
            return DispatchResult<AssignmentDto>.Fail(DispatchFailure.ParcelNotAssignable);
        if (!await couriers.IsEligibleAsync(courierId, ct))
            return DispatchResult<AssignmentDto>.Fail(DispatchFailure.CourierNotEligible);
        var courierProfile = await repository.GetCourierProfileAsync(courierId, ct);
        if (courierProfile is null || !courierProfile.IsAvailable)
            return DispatchResult<AssignmentDto>.Fail(DispatchFailure.CourierNotEligible);
        if (!await parcels.AssignAsync(parcelId, courierId, ct))
            return DispatchResult<AssignmentDto>.Fail(DispatchFailure.ParcelNotAssignable);

        var now = DateTimeOffset.UtcNow;
        assignment.CourierId = courierId;
        assignment.AssignedBy = assignedBy;
        assignment.AssignedAt = now;
        assignment.State = AssignmentStates.Assigned;
        assignment.Version++;
        try
        {
            await repository.SaveAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return DispatchResult<AssignmentDto>.Fail(DispatchFailure.ConcurrencyConflict);
        }

        await events.PublishAsync(new CourierAssignedEvent(Guid.NewGuid(), parcelId, courierId,
            assignment.TrackingNumber, now), ct);
        return DispatchResult<AssignmentDto>.Success(new(parcelId, courierId, assignedBy, now));
    }

    public async Task<CourierProfileResult> RegisterCourierAsync(Guid courierId, Guid? zoneId,
        string? vehicleType, string? licensePlate, CancellationToken ct)
    {
        if (!await couriers.IsActiveCourierAsync(courierId, ct))
            return CourierProfileResult.Fail(DispatchFailure.CourierNotEligible);
        if (await repository.GetCourierProfileAsync(courierId, ct) is not null)
            return CourierProfileResult.Fail(DispatchFailure.CourierAlreadyRegistered);
        if (zoneId.HasValue && !await zones.ExistsAsync(zoneId.Value, ct))
            return CourierProfileResult.Fail(DispatchFailure.ZoneNotFound);
        var profile = new CourierProfile
        {
            CourierId = courierId,
            ZoneId = zoneId,
            IsAvailable = true,
            VehicleType = vehicleType?.Trim(),
            LicensePlate = licensePlate?.Trim(),
            Version = 0
        };
        try
        {
            await repository.AddCourierProfileAsync(profile, ct);
            await repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return CourierProfileResult.Fail(DispatchFailure.CourierAlreadyRegistered);
        }
        return await ToDtoAsync(profile, ct);
    }

    public async Task<CourierProfileResult> UpdateCourierAsync(Guid courierId, Guid? zoneId, bool? isAvailable,
        string? vehicleType, string? licensePlate, CancellationToken ct)
    {
        var profile = await repository.GetCourierProfileAsync(courierId, ct);
        if (profile is null) return CourierProfileResult.Fail(DispatchFailure.CourierProfileNotFound);
        if (zoneId.HasValue)
        {
            if (!await zones.ExistsAsync(zoneId.Value, ct)) return CourierProfileResult.Fail(DispatchFailure.ZoneNotFound);
            profile.ZoneId = zoneId;
        }
        if (isAvailable.HasValue) profile.IsAvailable = isAvailable.Value;
        if (vehicleType is not null) profile.VehicleType = vehicleType.Trim();
        if (licensePlate is not null) profile.LicensePlate = licensePlate.Trim();
        profile.Version++;
        try { await repository.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return CourierProfileResult.Fail(DispatchFailure.ConcurrencyConflict); }
        return await ToDtoAsync(profile, ct);
    }

    public async Task<CourierProfilePage> ListCouriersAsync(Guid? zoneId, bool? isAvailable,
        int page, int pageSize, CancellationToken ct)
    {
        var (profiles, total) = await repository.ListCouriersAsync(zoneId, isAvailable, page, pageSize, ct);
        var ids = profiles.Select(p => p.CourierId).ToArray();
        var names = ids.Length == 0
            ? new Dictionary<Guid, string>()
            : await couriers.GetNamesAsync(ids, ct);
        var workloads = await repository.GetWorkloadsAsync(ids, ct);
        var items = profiles.Select(p => new CourierProfileDto(p.CourierId,
            names.GetValueOrDefault(p.CourierId, "Unknown courier"), p.ZoneId, p.IsAvailable,
            p.VehicleType, p.LicensePlate, workloads.GetValueOrDefault(p.CourierId))).ToList();
        return new(items, page, pageSize, total);
    }

    public async Task<CourierDashboardSummary> GetCourierSummaryAsync(CancellationToken ct)
    {
        var summary = await repository.GetCourierSummaryAsync(ct);
        return new(summary.Total, summary.Available, summary.Unavailable, summary.ActiveDeliveries);
    }

    public async Task<DispatchDashboardSummary> GetDashboardSummaryAsync(CancellationToken ct)
    {
        var summary = await repository.GetDashboardSummaryAsync(ct);
        return new(summary.Unassigned, summary.AvailableCouriers, summary.CouriersWithActiveDeliveries,
            summary.ActiveDeliveries, summary.TotalCouriers);
    }

    private async Task<CourierProfileResult> ToDtoAsync(CourierProfile profile, CancellationToken ct)
    {
        var names = await couriers.GetNamesAsync([profile.CourierId], ct);
        var workloads = await repository.GetWorkloadsAsync([profile.CourierId], ct);
        return CourierProfileResult.Success(new CourierProfileDto(profile.CourierId,
            names.GetValueOrDefault(profile.CourierId, "Unknown courier"), profile.ZoneId, profile.IsAvailable,
            profile.VehicleType, profile.LicensePlate, workloads.GetValueOrDefault(profile.CourierId)));
    }
}
