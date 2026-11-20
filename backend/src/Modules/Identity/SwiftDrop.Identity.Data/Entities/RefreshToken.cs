namespace SwiftDrop.Identity.Data.Entities;

/// A long-lived login that lets the browser get new access tokens without logging in again.
/// Only a SHA-256 hash of the token is stored, so a database leak does not reveal usable tokens.
public class RefreshToken
{
    private RefreshToken() { }   // required by EF Core

    public RefreshToken(Guid userId, string tokenHash, DateTime expiresAtUtc, bool isPersistent)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAtUtc = expiresAtUtc;
        IsPersistent = isPersistent;
    }

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; private set; }

    /// The user's "Remember me" choice. False means the cookie ends when the browser closes.
    public bool IsPersistent { get; private set; }

    /// Set when the token is used (rotated), when the user logs out, or when theft is suspected.
    public DateTime? RevokedAtUtc { get; private set; }

    public bool IsActive(DateTime nowUtc) => RevokedAtUtc is null && ExpiresAtUtc > nowUtc;

    public void Revoke(DateTime nowUtc) => RevokedAtUtc ??= nowUtc;
}
