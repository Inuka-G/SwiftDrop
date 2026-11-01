using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Data.Repositories;

public interface IZoneRepository
{
    Task<List<Zone>> GetAllAsync(CancellationToken ct);
    Task<Zone?> GetByIdAsync(Guid id, CancellationToken ct);
    Task AddAsync(Zone zone, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}