namespace SwiftDrop.Pricing.Contracts;

public record FeeEstimateDto(Guid ZoneId, string ZoneName, decimal BaseFee, decimal WeightFee, decimal Total);

public interface IPricingService
{
    Task<FeeEstimateDto?> EstimateAsync(Guid zoneId, decimal weightKg, ServiceType serviceType, CancellationToken ct = default);
}