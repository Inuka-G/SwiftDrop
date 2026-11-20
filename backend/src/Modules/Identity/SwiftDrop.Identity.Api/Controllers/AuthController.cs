using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Identity.Api.Dtos;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController(IAuthService auth, IRefreshTokenService refreshTokens) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct)
    {
        var result = await auth.LoginAsync(new LoginCommand(req.Username, req.Password), ct);
        RefreshTokenCookie.Set(Response, await refreshTokens.IssueAsync(result.User.Id, req.RememberMe, ct));
        return Ok(result);
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup(SignupRequest req, CancellationToken ct)
    {
        var result = await auth.SignupAsync(new SignupCommand(req.FullName, req.Email, req.Password, req.PhoneNumber), ct);
        RefreshTokenCookie.Set(Response, await refreshTokens.IssueAsync(result.User.Id, persistent: true, ct));
        return Created("/api/v1/auth/me", result);
    }

    /// Gets a new access token using the refresh-token cookie. The cookie is replaced each time (rotation).
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        try
        {
            var result = await refreshTokens.RefreshAsync(RefreshTokenCookie.Read(Request) ?? "", ct);
            RefreshTokenCookie.Set(Response, result.RefreshToken);   // keeps the original "Remember me" choice
            return Ok(result.Auth);
        }
        catch (UnauthorizedException)
        {
            RefreshTokenCookie.Delete(Response);
            throw;
        }
    }

    /// Ends this session on the server, so the refresh token cannot be used again.
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await refreshTokens.RevokeAsync(RefreshTokenCookie.Read(Request) ?? "", ct);
        RefreshTokenCookie.Delete(Response);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        return Ok(await auth.GetUserAsync(userId, ct));
    }
}
