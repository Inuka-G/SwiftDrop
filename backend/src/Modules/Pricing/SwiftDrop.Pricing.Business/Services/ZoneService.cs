using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Data.Repositories;

namespace SwiftDrop.Pricing.Business.Services;

internal sealed class ZoneService(IZoneRepository repo) : IZoneService
{
    public async Task<IReadOnlyList<ZoneDto>> GetAllAsync(CancellationToken ct) =>
        (await repo.GetAllAsync(ct)).Select(ToDto).ToList();

    public async Task<ZoneDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var zone = await repo.GetByIdAsync(id, ct);
        return zone is null ? null : ToDto(zone);
    }

    public async Task<ZoneDto> CreateAsync(CreateZoneCommand cmd, CancellationToken ct)
    {
        var zone = new Zone(cmd.ZoneName, cmd.Description);
        await repo.AddAsync(zone, ct);
        await repo.SaveChangesAsync(ct);
        return ToDto(zone);
    }

    public async Task<ZoneDto?> UpdateAsync(Guid id, UpdateZoneCommand cmd, CancellationToken ct)
    {
        var zone = await repo.GetByIdForUpdateAsync(id, ct);

        if (zone is null)
            return null;
        
        zone.UpdateDetails(cmd.ZoneName, cmd.Description);

        await repo.SaveChangesAsync(ct);

        return ToDto(zone);
    }

    public async Task<ZoneDto?> UpdateStatusAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var zone = await repo.GetByIdForUpdateAsync(id, ct);

        if (zone is null)
            return null;

        zone.SetActive(isActive);

        await repo.SaveChangesAsync(ct);

        return ToDto(zone);
    }

    public async Task<PricingRuleDto?> UpdatePricingRuleAsync(Guid zoneId, Guid ruleId, UpdatePricingRuleCommand cmd, CancellationToken ct)
    {
        if(cmd.BaseRate < 0 || cmd.AdditionalKgRate <0)
            throw new ArgumentException ("Pricing rates cannot be negative.");

        var rule = await repo.GetPricingRuleForUpdateAsync(zoneId, ruleId, ct);

        if (rule is null)
            return null;

        rule.UpdateRates(cmd.BaseRate, cmd.AdditionalKgRate);

        await repo.SaveChangesAsync(ct);

        return new PricingRuleDto(rule.Id, rule.ZoneId, rule.BaseRate, rule.AdditionalKgRate, rule.ServiceType, rule.IsActive);
    }

    private static ZoneDto ToDto(Zone z) => new(z.Id, z.ZoneName, z.Description, z.IsActive);
}