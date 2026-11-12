using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Parcels.Data.Entities;

/// Evidence that a parcel was handed over: the recipient's name, a photo, or both.
/// The photo itself lives in proof storage; only its generated key is stored here.
public class DeliveryProof : BaseEntity
{
    private DeliveryProof() { } // Required by EF Core

    public DeliveryProof(Guid parcelId, Guid courierId, string? recipientName, string? note,
        string? photoKey, string? photoContentType, DateTime capturedAtUtc)
    {
        ParcelId = parcelId;
        CourierId = courierId;
        RecipientName = recipientName;
        Note = note;
        PhotoKey = photoKey;
        PhotoContentType = photoContentType;
        CreatedAtUtc = capturedAtUtc;
    }

    public Guid ParcelId { get; private set; }
    public Guid CourierId { get; private set; }
    public string? RecipientName { get; private set; }
    public string? Note { get; private set; }
    public string? PhotoKey { get; private set; }
    public string? PhotoContentType { get; private set; }
}
