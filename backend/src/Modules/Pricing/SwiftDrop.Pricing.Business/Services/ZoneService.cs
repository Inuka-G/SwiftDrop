using Microsoft.EntityFrameworkCore;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Data.Repositories;
using SwiftDrop.SharedKernel.Exceptions;

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
        ValidatePricingValue(cmd.BaseRate, nameof(cmd.BaseRate));
        ValidatePricingValue(cmd.AdditionalKgRate, nameof(cmd.AdditionalKgRate));

        var rule = await repo.GetPricingRuleForUpdateAsync(zoneId, ruleId, ct);

        if (rule is null)
            return null;

        rule.UpdateRates(cmd.BaseRate, cmd.AdditionalKgRate);

        await repo.SaveChangesAsync(ct);

        return new PricingRuleDto(rule.Id, rule.ZoneId, rule.BaseRate, rule.AdditionalKgRate, rule.ServiceType, rule.IsActive);
    }

    public async Task<PricingRuleDto?> CreatePricingRuleAsync(Guid zoneId, CreatePricingRuleCommand cmd, CancellationToken ct)
    {
        if (!Enum.IsDefined(cmd.ServiceType))
            throw new BadRequestException("Invalid service type. Use Standard (1) or Express (2).");

        ValidatePricingValue(cmd.BaseRate, nameof(cmd.BaseRate));
        ValidatePricingValue(cmd.AdditionalKgRate, nameof(cmd.AdditionalKgRate));

        if (await repo.GetByIdAsync(zoneId, ct) is null)
            return null;

        if (await repo.PricingRuleExistsAsync(zoneId, cmd.ServiceType, ct))
            throw DuplicateRule(cmd.ServiceType);

        var rule = new PricingRule(zoneId, cmd.BaseRate, cmd.AdditionalKgRate, cmd.ServiceType, cmd.IsActive);
        await repo.AddPricingRuleAsync(rule, ct);

        try
        {
            await repo.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // The unique index is the final guard if concurrent requests race past the check above.
            if (await repo.PricingRuleExistsAsync(zoneId, cmd.ServiceType, ct))
                throw DuplicateRule(cmd.ServiceType);

            throw;
        }

        return new PricingRuleDto(rule.Id, rule.ZoneId, rule.BaseRate, rule.AdditionalKgRate, rule.ServiceType, rule.IsActive);
    }

    public async Task<IReadOnlyList<PricingRuleDto>> GetPricingRulesAsync(
    Guid zoneId,
    CancellationToken ct)
{
    var rules = await repo.GetPricingRulesAsync(zoneId, ct);

    return rules.Select(rule => new PricingRuleDto(
        rule.Id,
        rule.ZoneId,
        rule.BaseRate,
        rule.AdditionalKgRate,
        rule.ServiceType,
        rule.IsActive
    )).ToList();
}

    private static ZoneDto ToDto(Zone z) => new(z.Id, z.ZoneName, z.Description, z.IsActive);

    private static void ValidatePricingValue(decimal value, string name)
    {
        if (value < 0 || value > 99_999_999.99m || decimal.Round(value, 2) != value)
            throw new BadRequestException($"{name} must be between 0 and 99,999,999.99 with no more than two decimal places.");
    }

    private static ConflictException DuplicateRule(ServiceType serviceType) =>
        new($"A {serviceType} pricing rule already exists for this zone.");
}
