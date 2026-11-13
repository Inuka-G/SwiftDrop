using Microsoft.EntityFrameworkCore;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Persistence;

namespace SwiftDrop.Identity.Data.Repositories;

internal sealed class RefreshTokenRepository(IdentityDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct) =>
        db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, CancellationToken ct) =>
        await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAtUtc == null).ToListAsync(ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct) =>
        await db.RefreshTokens.AddAsync(token, ct);

    public Task SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
