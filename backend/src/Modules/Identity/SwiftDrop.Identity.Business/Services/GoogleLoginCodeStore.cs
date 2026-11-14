using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;

namespace SwiftDrop.Identity.Business.Services;

/// Keeps one-time codes that the React app swaps for a JWT after Google sign-in.
/// The JWT itself is never put in a URL, where it could end up in browser history or logs.
public interface IGoogleLoginCodeStore
{
    string Create(Guid userId);

    /// Returns the user for a valid code and deletes the code, so it works only once.
    Guid? Redeem(string code);
}

internal sealed class GoogleLoginCodeStore(IMemoryCache cache) : IGoogleLoginCodeStore
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(1);

    public string Create(Guid userId)
    {
        var code = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        cache.Set(Key(code), userId, Lifetime);
        return code;
    }

    public Guid? Redeem(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !cache.TryGetValue(Key(code), out Guid userId))
            return null;

        cache.Remove(Key(code));
        return userId;
    }

    private static string Key(string code) => $"google-login-code:{code}";
}
