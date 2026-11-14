using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using SwiftDrop.Identity.Api.Dtos;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.Api.Controllers;

/// "Login with Google" (OAuth 2.0 Authorization Code flow with PKCE).
/// 1. The browser opens /login, which redirects to Google.
/// 2. Google sends the browser back to /signin-google. ASP.NET Core checks the state, PKCE
///    and the signed ID token, then sends the browser to /complete.
/// 3. /complete finds or creates the customer and sends the browser to the React app with a one-time code.
/// 4. The React app posts that code to /exchange and gets a normal SwiftDrop JWT.
[ApiController]
[Route("api/v1/auth/google")]
[AllowAnonymous]
public class GoogleAuthController(IAuthService auth, IOptions<GoogleAuthOptions> options) : ControllerBase
{
    private const string CompletePath = "/api/v1/auth/google/complete";

    /// Open this in the browser (not with fetch or Swagger): it redirects to Google's sign-in page.
    [HttpGet("login")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public IActionResult Login()
    {
        EnsureConfigured();
        return Challenge(new AuthenticationProperties { RedirectUri = CompletePath }, GoogleAuthOptions.Scheme);
    }

    [HttpGet("complete")]
    [ApiExplorerSettings(IgnoreApi = true)]   // only reached through Google's redirect
    public async Task<IActionResult> Complete(CancellationToken ct)
    {
        EnsureConfigured();

        var result = await HttpContext.AuthenticateAsync(GoogleAuthOptions.ExternalCookieScheme);
        await HttpContext.SignOutAsync(GoogleAuthOptions.ExternalCookieScheme);   // the Google result is used once

        var googleUser = result.Principal;
        var googleUserId = googleUser?.FindFirst("sub")?.Value;
        var email = googleUser?.FindFirst("email")?.Value;
        if (!result.Succeeded || string.IsNullOrEmpty(googleUserId) || string.IsNullOrEmpty(email))
            return RedirectToFrontend("error", "Google sign-in failed. Please try again.");

        var command = new GoogleLoginCommand(
            googleUserId,
            email,
            googleUser!.FindFirst("name")?.Value,
            bool.TryParse(googleUser.FindFirst("email_verified")?.Value, out var verified) && verified);

        try
        {
            var code = await auth.CompleteGoogleLoginAsync(command, ct);
            return RedirectToFrontend("code", code);
        }
        catch (AppException ex)
        {
            // A browser redirect cannot show JSON, so the React page shows this (safe) message instead.
            return RedirectToFrontend("error", ex.Message);
        }
    }

    /// Called by the React app with the code from its URL. Returns the same result as normal login.
    [HttpPost("exchange")]
    [Produces("application/json")]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Exchange(GoogleExchangeRequest req, CancellationToken ct) =>
        Ok(await auth.ExchangeGoogleCodeAsync(req.Code, ct));

    private void EnsureConfigured()
    {
        if (!options.Value.IsConfigured)
            throw new ServiceUnavailableException("Google login is not set up on this server.");
    }

    private RedirectResult RedirectToFrontend(string key, string value) =>
        Redirect(QueryHelpers.AddQueryString(options.Value.FrontendCallbackUrl, key, value));
}
