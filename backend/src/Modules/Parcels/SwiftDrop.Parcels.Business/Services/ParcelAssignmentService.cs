using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Contracts.Events;

namespace SwiftDrop.Parcels.Business.Services;

internal sealed class ParcelAssignmentService(IParcelRepository repository, TimeProvider clock) : IParcelAssignment
{
    public async Task<CourierAssignmentResult?> AssignCourierAsync(Guid parcelId, Guid courierId, CancellationToken ct)
    {
        var parcel = await repository.GetByIdAsync(parcelId, ct);
        if (parcel is null || parcel.CurrentStatus != ParcelStatuses.Created || parcel.CourierId is not null)
            return null;

        var assignedAt = clock.GetUtcNow();
        var eventId = Guid.NewGuid();
        parcel.AssignCourier(courierId, assignedAt.UtcDateTime);
        repository.StageEvent(eventId, ParcelEventRoutingKeys.CourierAssigned,
            new CourierAssignedIntegrationEvent(eventId, parcel.Id, parcel.TrackingNumber, courierId,
                assignedAt.UtcDateTime), assignedAt.UtcDateTime);
        try
        {
            await repository.SaveChangesAsync(ct);
            return new CourierAssignmentResult(eventId, assignedAt);
        }
        catch (DbUpdateConcurrencyException)
        {
            return null;   // another dispatcher assigned it first
        }
    }
}
