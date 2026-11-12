using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Parcels.Data.Entities;

/// One row per status change. Rows are only ever added, never edited, so the
/// history is a reliable audit trail of who moved the parcel and when.
public class ParcelStatusHistory : BaseEntity
{
    private ParcelStatusHistory() { } // Required by EF Core

    public ParcelStatusHistory(Guid parcelId, string? fromStatus, string toStatus, string? note,
        Guid changedByUserId, string changedByRole, DateTime occurredAtUtc)
    {
        ParcelId = parcelId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Note = note;
        ChangedByUserId = changedByUserId;
        ChangedByRole = changedByRole;
        CreatedAtUtc = occurredAtUtc;
    }

    public Guid ParcelId { get; private set; }
    public string? FromStatus { get; private set; }
    public string ToStatus { get; private set; } = default!;
    public string? Note { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string ChangedByRole { get; private set; } = default!;
}
