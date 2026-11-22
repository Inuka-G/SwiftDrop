namespace SwiftDrop.Parcels.Contracts.Dtos;

public sealed record MerchantOrderDto(Guid ParcelId, string TrackingNumber, string RecipientName,
    string RecipientPhone, string DropoffAddress, string Status, decimal EstimatedFee, DateTime CreatedAtUtc);

public sealed record MerchantOrdersPage(IReadOnlyList<MerchantOrderDto> Items, int Page, int PageSize, int TotalCount);
