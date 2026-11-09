using SwiftDrop.Pricing.Contracts;
using SwiftDrop.Pricing.Data.Repositories;
using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Business.Services;

internal sealed class PricingService(IZoneRepository repo) : IPricingService
{
    public async Task<FeeEstimateDto?> EstimateAsync(Guid zoneId, decimal weightKg, ServiceType serviceType, CancellationToken ct = default)
    {
        var zone = await repo.GetByIdAsync(zoneId, ct);
        if (zone is null || !zone.IsActive) return null;

        var pricingRule = await repo.GetPricingRuleAsync(zoneId, serviceType, ct);

        if (pricingRule is null)
            return null;

        var additionalWeight = Math.Max(0, weightKg -1);

        var weightFee = additionalWeight * pricingRule.AdditionalKgRate;

        var totalFee = pricingRule.BaseRate + weightFee;

        return new FeeEstimateDto(
            zone.Id,
            zone.ZoneName,
            pricingRule.BaseRate,
            weightFee,
            totalFee
        );
    }
}