namespace SwiftDrop.Parcels.Contracts;

/// The parcel lifecycle: Created → PickedUp → InTransit → OutForDelivery → Delivered / Failed.
/// These strings are stored in the database and sent in events, so never rename them.
public static class ParcelStatuses
{
    public const string Created = "Created";
    public const string PickedUp = "PickedUp";
    public const string InTransit = "InTransit";
    public const string OutForDelivery = "OutForDelivery";
    public const string Delivered = "Delivered";
    public const string Failed = "Failed";

    public static readonly IReadOnlyList<string> All =
        [Created, PickedUp, InTransit, OutForDelivery, Delivered, Failed];

    public static bool IsTerminal(string status) => status is Delivered or Failed;
}

/// Why a courier could not deliver. "Other" needs a note explaining what happened.
public static class FailureReasons
{
    public const string RecipientUnavailable = "RecipientUnavailable";
    public const string WrongAddress = "WrongAddress";
    public const string RefusedByRecipient = "RefusedByRecipient";
    public const string DamagedParcel = "DamagedParcel";
    public const string AccessRestricted = "AccessRestricted";
    public const string Other = "Other";

    public static readonly IReadOnlyList<string> All =
        [RecipientUnavailable, WrongAddress, RefusedByRecipient, DamagedParcel, AccessRestricted, Other];
}
