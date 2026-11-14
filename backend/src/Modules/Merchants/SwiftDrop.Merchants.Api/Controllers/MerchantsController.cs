using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Merchants.Api.Controllers;

[ApiController]
[Route("api/v1/merchants")]
[Produces("application/json")]
public sealed class MerchantsController(IMerchantService merchantService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<MerchantDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register([FromBody] RegisterMerchantCommand command, CancellationToken ct)
    {
        var userId = User.GetUserId();
        var merchant = await merchantService.RegisterAsync(command, userId, ct);
        return CreatedAtAction(nameof(GetById), new { id = merchant.Id }, merchant);
    }

    [HttpGet("pending")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<IReadOnlyList<MerchantApprovalQueueItemDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingQueue(CancellationToken ct)
    {
        var pending = await merchantService.GetPendingQueueAsync(ct);
        return Ok(pending);
    }

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<MerchantDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        var adminId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var approved = await merchantService.ApproveAsync(id, adminId, ct);
        return Ok(approved);
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<MerchantDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken ct)
    {
        var adminId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var rejected = await merchantService.RejectAsync(id, adminId, ct);
        return Ok(rejected);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MerchantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyMerchantProfile(CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var merchant = await merchantService.GetByUserIdAsync(userId, ct);
        if (merchant is null)
            return NotFound(new { message = "No merchant profile found for current user." });

        return Ok(merchant);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<MerchantDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var merchant = await merchantService.GetByIdAsync(id, ct);
        return Ok(merchant);
    }

    [HttpPost("api-keys")]
    [Authorize]
    [ProducesResponseType<CreatedApiKeyDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyCommand command, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var merchant = await merchantService.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("You do not have a merchant profile.");

        var created = await merchantService.CreateApiKeyAsync(merchant.Id, command, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpGet("api-keys")]
    [Authorize]
    [ProducesResponseType<IReadOnlyList<ApiKeySummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApiKeys(CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var merchant = await merchantService.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("You do not have a merchant profile.");

        var keys = await merchantService.GetApiKeysAsync(merchant.Id, ct);
        return Ok(keys);
    }

    [HttpDelete("api-keys/{keyId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeApiKey(Guid keyId, CancellationToken ct)
    {
        var userId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var merchant = await merchantService.GetByUserIdAsync(userId, ct)
            ?? throw new NotFoundException("You do not have a merchant profile.");

        await merchantService.RevokeApiKeyAsync(merchant.Id, keyId, ct);
        return NoContent();
    }
}

