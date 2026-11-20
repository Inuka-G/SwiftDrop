using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.Business.Services;

public record IssuedRefreshToken(string Token, DateTime ExpiresAtUtc, bool IsPersistent);

public record RefreshResult(AuthResultDto Auth, IssuedRefreshToken RefreshToken);

public interface IRefreshTokenService
{
    /// Creates a new refresh token for a user who has just logged in.
    /// persistent = false ("Remember me" off) makes it last only until the browser closes.
    Task<IssuedRefreshToken> IssueAsync(Guid userId, bool persistent, CancellationToken ct);

    /// Swaps a refresh token for a new access token AND a new refresh token (rotation).
    Task<RefreshResult> RefreshAsync(string token, CancellationToken ct);

    /// Logs out: the token can never be used again. Unknown tokens are ignored.
    Task RevokeAsync(string token, CancellationToken ct);
}

/// Refresh tokens are rotated: each one works only once. If a token that was already used turns up
/// again, someone has copied it, so every refresh token of that user is revoked (they must log in again).
internal sealed class RefreshTokenService(
    IRefreshTokenRepository refreshTokens,
    IUserRepository users,
    ITokenService accessTokens,
    IOptions<JwtOptions> options) : IRefreshTokenService
{
    private const string InvalidMessage = "Your session has expired. Please log in again.";

    public async Task<IssuedRefreshToken> IssueAsync(Guid userId, bool persistent, CancellationToken ct)
    {
        var issued = await CreateAsync(userId, persistent, ct);
        await refreshTokens.SaveChangesAsync(ct);
        return issued;
    }

    public async Task<RefreshResult> RefreshAsync(string token, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var stored = string.IsNullOrWhiteSpace(token) ? null : await refreshTokens.FindByHashAsync(Hash(token), ct);
        if (stored is null)
            throw new UnauthorizedException(InvalidMessage);

        if (stored.RevokedAtUtc is not null)
        {
            // Reuse of an already-used token: treat as theft and end every session of this user.
            await RevokeAllAsync(stored.UserId, now, ct);
            throw new UnauthorizedException(InvalidMessage);
        }

        if (!stored.IsActive(now))
            throw new UnauthorizedException(InvalidMessage);

        var user = await users.GetByIdAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
        {
            await RevokeAllAsync(stored.UserId, now, ct);
            throw new UnauthorizedException(InvalidMessage);
        }

        stored.Revoke(now);
        var next = await CreateAsync(user.Id, stored.IsPersistent, ct);
        await refreshTokens.SaveChangesAsync(ct);

        var access = accessTokens.CreateToken(user);
        var dto = new UserDto(user.Id, user.FullName, user.Email, user.Username, user.Role.Name);
        return new RefreshResult(new AuthResultDto(access.Value, access.ExpiresAtUtc, dto), next);
    }

    public async Task RevokeAsync(string token, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var stored = await refreshTokens.FindByHashAsync(Hash(token), ct);
        if (stored is null) return;

        stored.Revoke(DateTime.UtcNow);
        await refreshTokens.SaveChangesAsync(ct);
    }

    private async Task<IssuedRefreshToken> CreateAsync(Guid userId, bool persistent, CancellationToken ct)
    {
        var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.AddDays(options.Value.RefreshTokenDays);
        await refreshTokens.AddAsync(new RefreshToken(userId, Hash(token), expiresAt, persistent), ct);
        return new IssuedRefreshToken(token, expiresAt, persistent);
    }

    private async Task RevokeAllAsync(Guid userId, DateTime now, CancellationToken ct)
    {
        foreach (var active in await refreshTokens.GetActiveForUserAsync(userId, ct))
            active.Revoke(now);
        await refreshTokens.SaveChangesAsync(ct);
    }

    internal static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
