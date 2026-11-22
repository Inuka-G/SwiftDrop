using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Business.Lifecycle;

internal static class StatusChange
{
    /// The one place a parcel's status changes, used by both the courier app and the
    /// dispatcher/admin endpoint: checks the state machine, then stages the new status,
    /// a history row and the ParcelStatusChanged event. The caller saves them in one transaction.
    public static void ApplyTransition(this IParcelRepository repository, Parcel parcel, string newStatus,
        string? note, Guid changedById, string changedByRole, string? failureReason, DateTime nowUtc)
    {
        var previous = parcel.CurrentStatus;
        if (!ParcelStatusTransitions.CanMove(previous, newStatus))
            throw new ConflictException($"A parcel in status {previous} cannot move to {newStatus}.");

        parcel.ChangeStatus(newStatus, nowUtc);
        repository.AddHistory(new ParcelStatusHistory(parcel.Id, previous, newStatus, note, changedById,
            changedByRole, nowUtc));

        // The free-text note stays in the history table: it can contain personal details.
        repository.StageEvent(new ParcelStatusChanged(parcel.Id, parcel.TrackingNumber, previous, newStatus,
            Note: null, parcel.CustomerId, MerchantId: null, parcel.CourierId, failureReason)
        {
            OccurredOnUtc = nowUtc
        });
    }
}
