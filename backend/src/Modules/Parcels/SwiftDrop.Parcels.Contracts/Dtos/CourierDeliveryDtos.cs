namespace SwiftDrop.Parcels.Contracts.Dtos;

/// One delivery as the assigned courier sees it. Contact details are included because
/// the courier needs them to pick up and hand over the parcel.
public sealed record CourierDeliveryDto(
    Guid Id,
    string TrackingNumber,
    string CurrentStatus,
    string PickupAddress,
    string PickupContactName,
    string PickupContactPhone,
    string DropoffAddress,
    string RecipientName,
    string RecipientPhone,
    decimal WeightKg,
    string SizeCategory,
    string PaymentMethod,
    decimal AmountToCollect,
    DateTime? AssignedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> AllowedNextStatuses);

public sealed record StatusHistoryEntryDto(
    string? FromStatus,
    string ToStatus,
    string? Note,
    string ChangedByRole,
    DateTime OccurredAtUtc);

public sealed record DeliveryProofDto(
    string? RecipientName,
    string? Note,
    bool HasPhoto,
    DateTime CapturedAtUtc);

public sealed record FailureReportDto(
    string Reason,
    string? Note,
    bool HasPhoto,
    DateTime ReportedAtUtc);

public sealed record UpdateDeliveryStatusRequest(string Status, string? Note);

/// Form fields for a failed delivery; the optional attempt photo arrives as a separate file part.
public sealed record ReportFailureRequest(string Reason, string? Note);

/// Form fields for proof of delivery; the optional photo arrives as a separate file part.
public sealed record ConfirmDeliveryRequest(string? RecipientName, string? Note);
