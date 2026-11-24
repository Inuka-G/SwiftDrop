using SwiftDrop.Parcels.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

// Records the assignment on the parcel through the Parcels public contract (in-process).
internal sealed class ParcelAssignmentGateway(IParcelAssignment parcels) : IParcelAssignmentGateway
{
    public Task<SwiftDrop.Parcels.Contracts.CourierAssignmentResult?> AssignAsync(Guid parcelId, Guid courierId, CancellationToken ct) =>
        parcels.AssignCourierAsync(parcelId, courierId, ct);
}
