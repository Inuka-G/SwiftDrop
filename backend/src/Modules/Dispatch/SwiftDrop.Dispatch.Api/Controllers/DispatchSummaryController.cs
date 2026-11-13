using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Dispatch.Api.Controllers;

[ApiController]
[Route("api/v1/dispatch")]
[Authorize(Roles = Roles.DispatcherOrAdmin)]
[Produces("application/json")]
public sealed class DispatchSummaryController(IDispatchService dispatch) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<DispatchDashboardSummary>(StatusCodes.Status200OK)]
    public async Task<ActionResult<DispatchDashboardSummary>> Get(CancellationToken ct) =>
        Ok(await dispatch.GetDashboardSummaryAsync(ct));
}
