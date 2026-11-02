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
public class AuthController(IAuthService auth) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest req, CancellationToken ct) =>
        Ok(await auth.LoginAsync(new LoginCommand(req.Username, req.Password), ct));

    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResultDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Signup(SignupRequest req, CancellationToken ct)
    {
        var result = await auth.SignupAsync(new SignupCommand(req.FullName, req.Email, req.Password, req.PhoneNumber), ct);
        return Created("/api/v1/auth/me", result);
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
