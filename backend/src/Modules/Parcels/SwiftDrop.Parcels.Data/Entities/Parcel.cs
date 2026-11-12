using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Parcels.Data.Entities;

public class Parcel : BaseEntity
{
    private Parcel() { } // Required by EF Core

    public Parcel(
        Guid customerId,
        string trackingNumber,
        Guid zoneId,
        string pickupAddress,
        string pickupContactName,
        string pickupContactPhone,
        string dropoffAddress,
        string recipientName,
        string recipientPhone,
        decimal weightKg,
        string sizeCategory,
        decimal estimatedFee,
        string paymentMethod)
    {
        CustomerId = customerId;
        TrackingNumber = trackingNumber;
        ZoneId = zoneId;
        PickupAddress = pickupAddress;
        PickupContactName = pickupContactName;
        PickupContactPhone = pickupContactPhone;
        DropoffAddress = dropoffAddress;
        RecipientName = recipientName;
        RecipientPhone = recipientPhone;
        WeightKg = weightKg;
        SizeCategory = sizeCategory;
        EstimatedFee = estimatedFee;
        PaymentMethod = paymentMethod;
        PaymentStatus = "Pending";
        CurrentStatus = "Created";
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public Guid CustomerId { get; private set; }
    public string TrackingNumber { get; private set; } = default!;
    public Guid ZoneId { get; private set; }

    // Pickup Details
    public string PickupAddress { get; private set; } = default!;
    public string PickupContactName { get; private set; } = default!;
    public string PickupContactPhone { get; private set; } = default!;

    // Drop-off Details
    public string DropoffAddress { get; private set; } = default!;
    public string RecipientName { get; private set; } = default!;
    public string RecipientPhone { get; private set; } = default!;

    // Parcel Specs & Pricing
    public decimal WeightKg { get; private set; }
    public string SizeCategory { get; private set; } = default!;
    public decimal EstimatedFee { get; private set; }
    public decimal? FinalFee { get; private set; }

    // Status & Payment
    public string PaymentMethod { get; private set; } = "COD";
    public string PaymentStatus { get; private set; } = "Pending";
    public string CurrentStatus { get; private set; } = "Created";
    public DateTime UpdatedAtUtc { get; private set; }
    public int Version { get; private set; } = 1;

    // Courier assignment (set by Dispatch through IParcelAssignment)
    public Guid? CourierId { get; private set; }
    public DateTime? AssignedAtUtc { get; private set; }

    // Transition rules live in ParcelStatusTransitions; these only record the result.
    public void AssignCourier(Guid courierId, DateTime nowUtc)
    {
        CourierId = courierId;
        AssignedAtUtc = nowUtc;
        Touch(nowUtc);
    }

    public void ChangeStatus(string newStatus, DateTime nowUtc)
    {
        CurrentStatus = newStatus;
        Touch(nowUtc);
    }

    // Bumping Version makes a concurrent update of the same parcel fail instead of overwriting.
    private void Touch(DateTime nowUtc)
    {
        UpdatedAtUtc = nowUtc;
        Version++;
    }
}