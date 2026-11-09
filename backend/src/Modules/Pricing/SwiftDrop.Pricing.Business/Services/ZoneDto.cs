using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Business.Services;

public record ZoneDto(Guid Id, string ZoneName, string? Description, bool IsActive);
public record CreateZoneCommand(string ZoneName, string? Description);
public record UpdateZoneCommand(string ZoneName, string? Description);
public record UpdatePricingRuleCommand(decimal BaseRate, decimal AdditionalKgRate);
public record PricingRuleDto(Guid Id, Guid ZoneId, decimal BaseRate, decimal AdditionalKgRate, ServiceType ServiceType, bool IsActive);

public interface IZoneService
{
    Task<IReadOnlyList<ZoneDto>> GetAllAsync(CancellationToken ct);
    Task<ZoneDto?> GetAsync(Guid id, CancellationToken ct);
    Task<ZoneDto> CreateAsync(CreateZoneCommand cmd, CancellationToken ct);
    Task<ZoneDto?> UpdateAsync(Guid id, UpdateZoneCommand cmd, CancellationToken ct);
    Task<ZoneDto?> UpdateStatusAsync(Guid id, bool isActive, CancellationToken ct);
    Task<PricingRuleDto?> UpdatePricingRuleAsync (Guid zoneId, Guid ruleId, UpdatePricingRuleCommand cmd, CancellationToken ct);
}