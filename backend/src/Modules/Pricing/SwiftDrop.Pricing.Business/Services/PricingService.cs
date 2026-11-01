using SwiftDrop.Pricing.Contracts;
using SwiftDrop.Pricing.Data.Repositories;

namespace SwiftDrop.Pricing.Business.Services;

internal sealed class PricingService(IZoneRepository repo) : IPricingService
{
    public async Task<FeeEstimateDto?> EstimateAsync(Guid zoneId, decimal weightKg, CancellationToken ct = default)
    {
        var zone = await repo.GetByIdAsync(zoneId, ct);
        if (zone is null || !zone.IsActive) return null;

        var weightFee = zone.PerKgRate * weightKg;
        return new FeeEstimateDto(zone.Id, zone.Name, zone.BaseFee, weightFee, zone.BaseFee + weightFee);
    }
}