using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Data.Repositories;

public interface IZoneRepository
{
    Task<List<Zone>> GetAllAsync(CancellationToken ct);
    Task<Zone?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Zone?> GetByIdForUpdateAsync(Guid id, CancellationToken ct);
    Task<PricingRule?> GetPricingRuleAsync(Guid zoneId, ServiceType serviceType, CancellationToken ct);
    Task<PricingRule?> GetPricingRuleForUpdateAsync(Guid zoneId, Guid ruleId, CancellationToken ct);
    Task AddAsync(Zone zone, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}