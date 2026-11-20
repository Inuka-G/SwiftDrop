namespace SwiftDrop.Parcels.Business.Lifecycle;

/// The signed-in user acting on a parcel, taken from the JWT "sub" and "role" claims.
public sealed record ParcelActor(Guid UserId, string Role);
