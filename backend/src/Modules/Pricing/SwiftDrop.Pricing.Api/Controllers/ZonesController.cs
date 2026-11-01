using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Pricing.Api.Dtos;
using SwiftDrop.Pricing.Business.Services;
using SwiftDrop.Pricing.Contracts;

namespace SwiftDrop.Pricing.Api.Controllers;

[ApiController]
[Route("api/v1/zones")]
[Produces("application/json")]
public class ZonesController(IZoneService zones, IPricingService pricing) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ZoneDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await zones.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ZoneDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
    {
        var zone = await zones.GetAsync(id, ct);
        return zone is null ? NotFound() : Ok(zone);
    }

    [HttpPost]   // TODO: [Authorize(Roles = Roles.Admin)] once Identity is done
    [ProducesResponseType<ZoneDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateZoneRequest req, CancellationToken ct)
    {
        var created = await zones.CreateAsync(new CreateZoneCommand(req.Name, req.BaseFee, req.PerKgRate), ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/estimate")]
    [ProducesResponseType<FeeEstimateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Estimate(Guid id, [FromQuery] decimal weightKg, CancellationToken ct)
    {
        var fee = await pricing.EstimateAsync(id, weightKg, ct);
        return fee is null ? NotFound() : Ok(fee);
    }
}