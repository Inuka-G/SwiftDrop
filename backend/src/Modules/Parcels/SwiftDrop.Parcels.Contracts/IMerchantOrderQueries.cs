using SwiftDrop.Parcels.Contracts.Dtos;

namespace SwiftDrop.Parcels.Contracts;

public interface IMerchantOrderQueries
{
    Task<MerchantOrdersPage> ListMerchantOrdersAsync(Guid merchantId, string? status, int page, int pageSize, CancellationToken ct);
    Task<MerchantOrderDto?> GetMerchantOrderAsync(Guid merchantId, Guid parcelId, CancellationToken ct);
}
