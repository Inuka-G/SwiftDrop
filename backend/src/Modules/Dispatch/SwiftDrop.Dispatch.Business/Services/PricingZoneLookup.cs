using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Dispatch.Business.Services;

/// A zone is dispatchable when Pricing can produce an estimate for its standard service.
internal sealed class PricingZoneLookup(IPricingService pricing) : IZoneLookup
{
    public async Task<bool> ExistsAsync(Guid zoneId, CancellationToken ct) =>
        await pricing.EstimateAsync(zoneId, 1m, ServiceType.Standard, ct) is not null;
}
