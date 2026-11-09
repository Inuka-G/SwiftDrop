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

    public Task<PricingRule?> GetPricingRuleAsync(Guid zoneId, ServiceType serviceType, CancellationToken ct) =>
        db.PricingRules.AsNoTracking().FirstOrDefaultAsync(p => p.ZoneId == zoneId && p.ServiceType == serviceType && p.IsActive, ct);


    public async Task AddAsync(Zone zone, CancellationToken ct) =>
        await db.Zones.AddAsync(zone, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}