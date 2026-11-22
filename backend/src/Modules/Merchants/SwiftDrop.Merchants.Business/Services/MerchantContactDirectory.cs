using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Merchants.Data.Repositories;
using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Business.Services;

internal sealed class MerchantContactDirectory(IMerchantRepository repository) : IMerchantContactDirectory
{
    public async Task<Guid?> GetApprovedContactUserIdAsync(Guid id, CancellationToken ct)
    {
        var merchant = await repository.GetByIdAsync(id, ct);
        return merchant is { Status: MerchantStatus.Approved } ? merchant.UserId : null;
    }
}
