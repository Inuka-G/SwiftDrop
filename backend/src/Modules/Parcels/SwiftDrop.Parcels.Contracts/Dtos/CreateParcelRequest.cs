namespace SwiftDrop.Parcels.Contracts.Dtos;

public sealed record CreateParcelRequest(
    Guid ZoneId,
    string PickupAddress,
    string PickupContactName,
    string PickupContactPhone,
    string DropoffAddress,
    string RecipientName,
    string RecipientPhone,
    decimal WeightKg,
    string SizeCategory,
    string PaymentMethod = "COD"
);