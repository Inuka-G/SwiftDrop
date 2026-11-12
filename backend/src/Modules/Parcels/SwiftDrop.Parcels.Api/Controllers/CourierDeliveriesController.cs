using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Api.Controllers;

/// The signed-in courier's own deliveries. Every action checks that the parcel is
/// assigned to the caller, so a courier can never see or change another courier's work.
[ApiController]
[Route("api/v1/courier/deliveries")]
[Authorize(Roles = Roles.Courier)]
[Produces("application/json")]
public sealed class CourierDeliveriesController(
    IParcelLifecycleService lifecycle,
    IValidator<UpdateDeliveryStatusRequest> statusValidator,
    IValidator<ConfirmDeliveryRequest> deliverValidator,
    IValidator<ReportFailureRequest> failValidator) : ControllerBase
{
    private const long MaxDeliverRequestBytes = 6 * 1024 * 1024;   // 5 MB photo plus form fields

    /// Open deliveries plus those closed since closedSince (default: start of today, UTC).
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CourierDeliveryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] DateTimeOffset? closedSince, CancellationToken ct)
    {
        var since = closedSince?.UtcDateTime ?? DateTime.UtcNow.Date;
        return Ok(await lifecycle.GetCourierDeliveriesAsync(CourierId, since, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CourierDeliveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) =>
        Ok(await lifecycle.GetCourierDeliveryAsync(CourierId, id, ct));

    /// Moves the parcel to PickedUp, InTransit or OutForDelivery.
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<CourierDeliveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateDeliveryStatusRequest request, CancellationToken ct)
    {
        await ValidateAsync(statusValidator, request, ct);
        return Ok(await lifecycle.UpdateStatusAsync(CourierId, id, request, ct));
    }

    /// Marks the parcel Delivered with proof: the recipient's name, a photo, or both.
    [HttpPost("{id:guid}/deliver")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDeliverRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxDeliverRequestBytes)]
    [ProducesResponseType<CourierDeliveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deliver(Guid id, [FromForm] ConfirmDeliveryRequest request, IFormFile? photo,
        CancellationToken ct)
    {
        await ValidateAsync(deliverValidator, request, ct);
        return Ok(await WithPhotoAsync(photo, upload => lifecycle.ConfirmDeliveryAsync(CourierId, id, request, upload, ct)));
    }

    /// Marks the parcel Failed with a reason and an optional photo of the attempt.
    [HttpPost("{id:guid}/fail")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxDeliverRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxDeliverRequestBytes)]
    [ProducesResponseType<CourierDeliveryDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Fail(Guid id, [FromForm] ReportFailureRequest request, IFormFile? photo,
        CancellationToken ct)
    {
        await ValidateAsync(failValidator, request, ct);
        return Ok(await WithPhotoAsync(photo, upload => lifecycle.ReportFailureAsync(CourierId, id, request, upload, ct)));
    }

    private Guid CourierId => User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");

    // Keeps the upload stream open for exactly as long as the service needs it.
    private static async Task<T> WithPhotoAsync<T>(IFormFile? photo, Func<ProofPhotoUpload?, Task<T>> action)
    {
        if (photo is null) return await action(null);
        await using var stream = photo.OpenReadStream();
        return await action(new ProofPhotoUpload(stream, photo.Length));
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid) throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
