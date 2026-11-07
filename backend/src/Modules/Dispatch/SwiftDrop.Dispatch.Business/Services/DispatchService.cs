using Microsoft.EntityFrameworkCore;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.Business.Services;

internal sealed class DispatchService(IDispatchRepository repository,
    ICourierEligibilityGateway couriers, IParcelAssignmentGateway parcels,
    IDispatchEventPublisher events) : IDispatchService
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
}
