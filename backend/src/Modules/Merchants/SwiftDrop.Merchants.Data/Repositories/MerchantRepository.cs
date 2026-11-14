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

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

