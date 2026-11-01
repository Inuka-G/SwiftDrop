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
        var zone = new Zone(cmd.Name, cmd.BaseFee, cmd.PerKgRate);
        await repo.AddAsync(zone, ct);
        await repo.SaveChangesAsync(ct);
        return ToDto(zone);
    }

    private static ZoneDto ToDto(Zone z) => new(z.Id, z.Name, z.BaseFee, z.PerKgRate, z.IsActive);
}