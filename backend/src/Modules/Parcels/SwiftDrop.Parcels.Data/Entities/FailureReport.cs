using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Parcels.Data.Entities;

/// Why a delivery failed, as reported by the courier, with an optional photo of the
/// attempt (for example a locked gate). The photo lives in proof storage.
public class FailureReport : BaseEntity
{
    private FailureReport() { } // Required by EF Core

    public FailureReport(Guid parcelId, Guid courierId, string reason, string? note,
        string? photoKey, string? photoContentType, DateTime reportedAtUtc)
    {
        ParcelId = parcelId;
        CourierId = courierId;
        Reason = reason;
        Note = note;
        PhotoKey = photoKey;
        PhotoContentType = photoContentType;
        CreatedAtUtc = reportedAtUtc;
    }

    public Guid ParcelId { get; private set; }
    public Guid CourierId { get; private set; }
    public string Reason { get; private set; } = default!;
    public string? Note { get; private set; }
    public string? PhotoKey { get; private set; }
    public string? PhotoContentType { get; private set; }
}
