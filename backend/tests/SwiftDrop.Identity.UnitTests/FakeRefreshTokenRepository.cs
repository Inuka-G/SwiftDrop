using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;

namespace SwiftDrop.Identity.UnitTests;

internal sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Tokens { get; } = [];

    public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct) =>
        Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(Tokens.Where(t => t.UserId == userId && t.RevokedAtUtc == null).ToList());

    public Task AddAsync(RefreshToken token, CancellationToken ct)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}
