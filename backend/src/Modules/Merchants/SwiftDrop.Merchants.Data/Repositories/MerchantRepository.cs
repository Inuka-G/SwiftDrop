using Microsoft.EntityFrameworkCore;
using SwiftDrop.Merchants.Data.Entities;
using SwiftDrop.Merchants.Data.Persistence;

namespace SwiftDrop.Merchants.Data.Repositories;

internal sealed class MerchantRepository(MerchantsDbContext db) : IMerchantRepository
{
    public Task<Merchant?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Merchants.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<Merchant?> GetByRegNumberAsync(string regNumber, CancellationToken ct = default)
    {
        var normalized = regNumber.Trim().ToUpperInvariant();
        return db.Merchants.FirstOrDefaultAsync(m => m.BusinessRegNumber == normalized, ct);
    }

    public Task<Merchant?> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        db.Merchants.FirstOrDefaultAsync(m => m.UserId == userId, ct);

    public async Task<IReadOnlyList<Merchant>> GetPendingAsync(CancellationToken ct = default) =>
        await db.Merchants
            .Where(m => m.Status == MerchantStatus.Pending)
            .OrderBy(m => m.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Merchant> Items, int TotalCount)> ListAsync(
        string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.Merchants.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(m => m.Status == status);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(Merchant merchant, CancellationToken ct = default) =>
        await db.Merchants.AddAsync(merchant, ct);

    public async Task AddApiKeyAsync(ApiKey key, CancellationToken ct = default) =>
        await db.ApiKeys.AddAsync(key, ct);

    public Task<ApiKey?> GetApiKeyByHashAsync(string keyHash, CancellationToken ct = default) =>
        db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == keyHash, ct);

    public async Task<IReadOnlyList<ApiKey>> GetApiKeysByMerchantIdAsync(Guid merchantId, CancellationToken ct = default) =>
        await db.ApiKeys
            .Where(k => k.MerchantId == merchantId)
            .OrderByDescending(k => k.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<ApiKey?> GetApiKeyByIdAsync(Guid keyId, CancellationToken ct = default) =>
        db.ApiKeys.FirstOrDefaultAsync(k => k.Id == keyId, ct);

    public async Task AddClientCredentialAsync(ClientCredential credential, CancellationToken ct = default) =>
        await db.ClientCredentials.AddAsync(credential, ct);

    public Task<ClientCredential?> GetClientCredentialByClientIdAsync(string clientId, CancellationToken ct = default) =>
        db.ClientCredentials.FirstOrDefaultAsync(c => c.ClientId == clientId, ct);

    public async Task<IReadOnlyList<ClientCredential>> GetClientCredentialsByMerchantIdAsync(Guid merchantId, CancellationToken ct = default) =>
        await db.ClientCredentials
            .Where(c => c.MerchantId == merchantId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync(ct);

    public Task<ClientCredential?> GetClientCredentialByIdAsync(Guid id, CancellationToken ct = default) =>
        db.ClientCredentials.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

