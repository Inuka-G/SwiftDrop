using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.Api.Controllers;

[ApiController]
[Route("api/v1/merchant/orders")]
[Produces("application/json")]
public sealed class MerchantOrdersController(
    IMerchantAccess merchantAccess,
    IParcelService parcelService,
    IValidator<CreateParcelRequest> validator) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ParcelCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromHeader(Name = "X-Api-Key")] string? apiKey,
        CreateParcelRequest request, CancellationToken ct)
    {
        var merchant = string.IsNullOrWhiteSpace(apiKey) ? null : await merchantAccess.ValidateApiKeyAsync(apiKey, ct);
        if (merchant is not { IsActive: true } || merchant.MerchantId == Guid.Empty)
            throw new UnauthorizedException("A valid merchant API key is required.");

        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            throw new BadRequestException(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage)));

        var response = await parcelService.CreateMerchantParcelAsync(merchant.MerchantId, request, ct);
        return CreatedAtAction(nameof(ParcelsController.Track), "Parcels",
            new { trackingNumber = response.TrackingNumber }, response);
    }
}
