using static SwiftDrop.Parcels.Contracts.ParcelStatuses;

namespace SwiftDrop.Parcels.Business.Lifecycle;

/// The parcel state machine. Every status change goes through CanMove, so an
/// invalid jump (for example Created → Delivered) is impossible from any endpoint.
public static class ParcelStatusTransitions
{
    private static readonly IReadOnlyDictionary<string, string[]> Allowed = new Dictionary<string, string[]>
    {
        [Created] = [PickedUp, Failed],
        [PickedUp] = [InTransit, Failed],
        [InTransit] = [OutForDelivery, Failed],
        [OutForDelivery] = [Delivered, Failed],
        [Delivered] = [],
        [Failed] = [],
    };

    public static IReadOnlyList<string> NextFrom(string current) =>
        Allowed.TryGetValue(current, out var next) ? next : [];

    public static bool CanMove(string from, string to) => NextFrom(from).Contains(to);
}
