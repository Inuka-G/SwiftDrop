using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Api.Controllers;

/// Shows role-based access control working: each endpoint allows one role.
/// Remove this once the real role-protected endpoints from the other modules exist.
[ApiController]
[Route("api/v1/auth/role-check")]
[Produces("application/json")]
public class RoleCheckController : ControllerBase
{
    [HttpGet("customer"), Authorize(Roles = Roles.Customer)]
    public IActionResult Customer() => Ok(new { allowed = Roles.Customer });

    [HttpGet("merchant"), Authorize(Roles = Roles.Merchant)]
    public IActionResult Merchant() => Ok(new { allowed = Roles.Merchant });

    [HttpGet("courier"), Authorize(Roles = Roles.Courier)]
    public IActionResult Courier() => Ok(new { allowed = Roles.Courier });

    [HttpGet("dispatcher-or-admin"), Authorize(Roles = Roles.DispatcherOrAdmin)]
    public IActionResult DispatcherOrAdmin() => Ok(new { allowed = Roles.DispatcherOrAdmin });
}
