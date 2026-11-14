using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Data.Repositories;

public interface IMerchantRepository
{
    Task<Merchant?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Merchant?> GetByRegNumberAsync(string regNumber, CancellationToken ct = default);
    Task<Merchant?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<Merchant>> GetPendingAsync(CancellationToken ct = default);
    Task<(IReadOnlyList<Merchant> Items, int TotalCount)> ListAsync(string? status, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Merchant merchant, CancellationToken ct = default);
    Task AddApiKeyAsync(ApiKey key, CancellationToken ct = default);
    Task<ApiKey?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetApiKeysByMerchantIdAsync(Guid merchantId, CancellationToken ct = default);
    Task<ApiKey?> GetApiKeyByIdAsync(Guid keyId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

