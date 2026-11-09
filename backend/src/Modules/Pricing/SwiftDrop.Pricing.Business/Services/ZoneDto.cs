namespace SwiftDrop.Pricing.Business.Services;

public record ZoneDto(Guid Id, string ZoneName, string? Description, bool IsActive);
public record CreateZoneCommand(string ZoneName, string? Description);

public interface IZoneService
{
    Task<IReadOnlyList<ZoneDto>> GetAllAsync(CancellationToken ct);
    Task<ZoneDto?> GetAsync(Guid id, CancellationToken ct);
    Task<ZoneDto> CreateAsync(CreateZoneCommand cmd, CancellationToken ct);
}