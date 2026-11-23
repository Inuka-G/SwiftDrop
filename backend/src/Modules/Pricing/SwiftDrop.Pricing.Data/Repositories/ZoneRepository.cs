using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Data.Persistence;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Data.Repositories;

internal sealed class ZoneRepository(PricingDbContext db) : IZoneRepository
{
    public Task<List<Zone>> GetAllAsync(CancellationToken ct) =>
        db.Zones.AsNoTracking().OrderBy(z => z.ZoneName).ToListAsync(ct);

    public Task<Zone?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Zones.AsNoTracking().FirstOrDefaultAsync(z => z.Id == id, ct);

    public Task<Zone?> GetByIdForUpdateAsync(Guid id, CancellationToken ct) =>
        db.Zones.FirstOrDefaultAsync(z => z.Id == id, ct);

    public Task<PricingRule?> GetPricingRuleAsync(Guid zoneId, ServiceType serviceType, CancellationToken ct) =>
        db.PricingRules.AsNoTracking().FirstOrDefaultAsync(p => p.ZoneId == zoneId && p.ServiceType == serviceType && p.IsActive, ct);

    public Task<PricingRule?> GetPricingRuleForUpdateAsync(Guid zoneId, Guid ruleId, CancellationToken ct)
    {
        return db.PricingRules.FirstOrDefaultAsync(r => r.Id == ruleId && r.ZoneId == zoneId, ct);
    }

    public Task<bool> PricingRuleExistsAsync(Guid zoneId, ServiceType serviceType, CancellationToken ct) =>
        db.PricingRules.AnyAsync(r => r.ZoneId == zoneId && r.ServiceType == serviceType, ct);

    public async Task AddAsync(Zone zone, CancellationToken ct) =>
        await db.Zones.AddAsync(zone, ct);

    public async Task AddPricingRuleAsync(PricingRule pricingRule, CancellationToken ct) =>
        await db.PricingRules.AddAsync(pricingRule, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<List<PricingRule>> GetPricingRulesAsync(
    Guid zoneId,
    CancellationToken ct)
{
    return db.PricingRules
        .AsNoTracking()
        .Where(r => r.ZoneId == zoneId)
        .OrderBy(r => r.ServiceType)
        .ToListAsync(ct);
}
}
