using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Data.Repositories;

namespace SwiftDrop.Parcels.Business.Services;

internal sealed class ParcelAssignmentService(IParcelRepository repository, TimeProvider clock) : IParcelAssignment
{
    public async Task<bool> AssignCourierAsync(Guid parcelId, Guid courierId, CancellationToken ct)
    {
        var parcel = await repository.GetByIdAsync(parcelId, ct);
        if (parcel is null || parcel.CurrentStatus != ParcelStatuses.Created || parcel.CourierId is not null)
            return false;

        parcel.AssignCourier(courierId, clock.GetUtcNow().UtcDateTime);
        try
        {
            await repository.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;   // another dispatcher assigned it first
        }
    }
}
