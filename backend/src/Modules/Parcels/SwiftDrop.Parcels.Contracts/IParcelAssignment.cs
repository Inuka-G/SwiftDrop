namespace SwiftDrop.Parcels.Contracts;

/// Called in-process by Dispatch when a dispatcher assigns a parcel, so the assignment
/// succeeds or fails immediately. Only a parcel that is still Created and unassigned
/// can be assigned; returns false otherwise.
public interface IParcelAssignment
{
    Task<bool> AssignCourierAsync(Guid parcelId, Guid courierId, CancellationToken ct);
}
