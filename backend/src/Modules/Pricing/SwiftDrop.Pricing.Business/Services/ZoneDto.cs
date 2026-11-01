namespace SwiftDrop.Pricing.Business.Services;

public record ZoneDto(Guid Id, string Name, decimal BaseFee, decimal PerKgRate, bool IsActive);
public record CreateZoneCommand(string Name, decimal BaseFee, decimal PerKgRate);

public interface IZoneService
{
    Task<IReadOnlyList<ZoneDto>> GetAllAsync(CancellationToken ct);
    Task<ZoneDto?> GetAsync(Guid id, CancellationToken ct);
    Task<ZoneDto> CreateAsync(CreateZoneCommand cmd, CancellationToken ct);
}