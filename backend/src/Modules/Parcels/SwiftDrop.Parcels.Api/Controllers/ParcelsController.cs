using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Parcels.Api.Services;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Api.Controllers;

[ApiController]
[Route("api/v1/parcels")]
[Produces("application/json")]
public sealed class ParcelsController(
    IParcelService parcelService,
    IValidator<CreateParcelRequest> validator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType<ParcelCreatedResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateParcelRequest request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new BadRequestException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var customerId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var response = await parcelService.CreateParcelAsync(customerId, request, ct);

        return CreatedAtAction(nameof(Track), new { trackingNumber = response.TrackingNumber }, response);
    }

    [HttpGet("my-history")]
    [Authorize(Roles = Roles.Customer)]
    [ProducesResponseType<IReadOnlyList<ParcelSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyHistory(CancellationToken ct)
    {
        var customerId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var history = await parcelService.GetCustomerHistoryAsync(customerId, ct);
        return Ok(history);
    }

    [HttpGet("track/{trackingNumber}")]
    [AllowAnonymous]
    [ProducesResponseType<PublicTrackingDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Track(string trackingNumber, CancellationToken ct)
    {
        var tracking = await parcelService.GetPublicTrackingAsync(trackingNumber, ct);
        return tracking is null ? NotFound(new { message = "Parcel not found." }) : Ok(tracking);
    }

    [HttpGet("{id:guid}/receipt")]
    [Authorize(Roles = Roles.Customer)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadReceipt(Guid id, CancellationToken ct)
    {
        var customerId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var details = await parcelService.GetReceiptDetailsAsync(id, customerId, ct);
        if (details is null) return NotFound(new { message = "Parcel receipt not found." });

        var pdfBytes = ReceiptPdfGenerator.Generate(details);
        return File(pdfBytes, "application/pdf", $"SwiftDrop-Receipt-{details.TrackingNumber}.pdf");
    }
}