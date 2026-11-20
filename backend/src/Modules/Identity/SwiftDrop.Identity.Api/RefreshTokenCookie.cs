using Microsoft.AspNetCore.Http;
using SwiftDrop.Identity.Business.Services;

namespace SwiftDrop.Identity.Api;

/// The refresh token lives in an HttpOnly cookie: JavaScript (and therefore an XSS attack) cannot read it,
/// and the browser only sends it to the /api/v1/auth endpoints.
public static class RefreshTokenCookie
{
    public const string Name = "swiftdrop_refresh";
    public const string Path = "/api/v1/auth";

    /// A token issued with "Remember me" off becomes a session cookie that disappears when the browser closes.
    public static void Set(HttpResponse response, IssuedRefreshToken token) =>
        response.Cookies.Append(Name, token.Token,
            Options(response.HttpContext.Request, token.IsPersistent ? token.ExpiresAtUtc : null));

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Delete(HttpResponse response) =>
        response.Cookies.Delete(Name, Options(response.HttpContext.Request, null));

    private static CookieOptions Options(HttpRequest request, DateTime? expiresAtUtc) => new()
    {
        HttpOnly = true,
        Secure = request.IsHttps,          // always true in production (HTTPS)
        SameSite = SameSiteMode.Strict,    // never sent with requests started by other websites
        Path = Path,
        Expires = expiresAtUtc,
        IsEssential = true,
    };
}
