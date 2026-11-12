using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Api.Controllers;

/// Read access to a parcel's lifecycle. Allowed for the owning customer, the assigned
/// courier, and dispatchers/admins; the service checks which of these the caller is.
[ApiController]
[Route("api/v1/parcels/{id:guid}")]
[Authorize]
[Produces("application/json")]
public sealed class ParcelLifecycleController(IParcelLifecycleService lifecycle) : ControllerBase
{
    [HttpGet("history")]
    [ProducesResponseType<IReadOnlyList<StatusHistoryEntryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> History(Guid id, CancellationToken ct) =>
        Ok(await lifecycle.GetHistoryAsync(Actor, id, ct));

    [HttpGet("proof")]
    [ProducesResponseType<DeliveryProofDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Proof(Guid id, CancellationToken ct)
    {
        var proof = await lifecycle.GetProofAsync(Actor, id, ct);
        return proof is null ? throw new NotFoundException("This parcel has no proof of delivery.") : Ok(proof);
    }

    [HttpGet("proof/photo")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProofPhoto(Guid id, CancellationToken ct) =>
        PhotoResult(await lifecycle.OpenProofPhotoAsync(Actor, id, ct), "This parcel has no proof photo.");

    [HttpGet("failure/photo")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> FailurePhoto(Guid id, CancellationToken ct) =>
        PhotoResult(await lifecycle.OpenFailurePhotoAsync(Actor, id, ct), "This parcel has no failure photo.");

    private FileStreamResult PhotoResult(ProofPhoto? photo, string notFoundMessage)
    {
        if (photo is null) throw new NotFoundException(notFoundMessage);
        Response.Headers.CacheControl = "private, no-store";   // photos can show people and homes
        return File(photo.Content, photo.ContentType);
    }

    [HttpGet("failure")]
    [ProducesResponseType<FailureReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Failure(Guid id, CancellationToken ct)
    {
        var report = await lifecycle.GetFailureReportAsync(Actor, id, ct);
        return report is null ? throw new NotFoundException("This parcel has no failure report.") : Ok(report);
    }

    private ParcelActor Actor => new(
        User.GetUserId() ?? throw new UnauthorizedException("The token has no user id."),
        User.FindFirstValue("role") ?? string.Empty);
}
