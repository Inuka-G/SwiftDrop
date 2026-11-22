using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Merchants.Data.Entities;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Merchants.Api.Controllers;

[ApiController]
[Route("api/v1/merchant/orders")]
[Produces("application/json")]
public sealed class MerchantOrderQueriesController(
    IMerchantAccess merchantAccess,
    IMerchantService merchantService,
    IMerchantOrderQueries orders) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<MerchantOrdersPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<MerchantOrdersPage>> List(
        [FromHeader(Name = "X-Api-Key")] string? apiKey, CancellationToken ct,
        [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
    {
        var merchantId = await RequireMerchantIdAsync(apiKey, ct);
        return Ok(await orders.ListMerchantOrdersAsync(merchantId, status, page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MerchantOrderDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MerchantOrderDto>> Get(Guid id,
        [FromHeader(Name = "X-Api-Key")] string? apiKey, CancellationToken ct)
    {
        var merchantId = await RequireMerchantIdAsync(apiKey, ct);
        var order = await orders.GetMerchantOrderAsync(merchantId, id, ct)
            ?? throw new NotFoundException("Merchant order not found.");
        return Ok(order);
    }

    private async Task<Guid> RequireMerchantIdAsync(string? apiKey, CancellationToken ct)
    {
        if (Request.Headers.TryGetValue("X-Api-Key", out var values))
        {
            var merchant = values.Count == 1 && !string.IsNullOrWhiteSpace(apiKey)
                ? await merchantAccess.ValidateApiKeyAsync(apiKey, ct)
                : null;
            if (merchant is not { IsActive: true } || merchant.MerchantId == Guid.Empty)
                throw new UnauthorizedException("A valid merchant API key is required.");

            return merchant.MerchantId;
        }

        if (User.Identity?.IsAuthenticated != true || User.GetUserId() is not { } userId
            || User.HasClaim(claim => claim.Type is "client_id" or "merchant_id"))
            throw new UnauthorizedException("A valid merchant API key or browser login is required.");

        var profile = await merchantService.GetByUserIdAsync(userId, ct);
        if (profile is null || profile.Status != MerchantStatus.Approved)
            throw new ForbiddenException("An approved merchant profile is required.");

        return profile.Id;
    }
}
