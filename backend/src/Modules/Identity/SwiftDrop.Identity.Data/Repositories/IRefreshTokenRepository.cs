using SwiftDrop.Identity.Data.Entities;

namespace SwiftDrop.Identity.Data.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct);
    Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, CancellationToken ct);
    Task AddAsync(RefreshToken token, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
