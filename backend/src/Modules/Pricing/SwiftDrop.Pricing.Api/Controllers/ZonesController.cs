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
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var zone = await zones.GetAsync(id, ct);
        return zone is null ? NotFound() : Ok(zone);
    }

    [HttpPost]   // TODO: [Authorize(Roles = Roles.Admin)] once Identity is done
    [ProducesResponseType<ZoneDto>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateZoneRequest req, CancellationToken ct)
    {
        var created = await zones.CreateAsync(new CreateZoneCommand(req.ZoneName, req.Description), ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpGet("{id:guid}/estimate")]
    [ProducesResponseType<FeeEstimateDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Estimate(Guid id, [FromQuery] decimal weightKg, [FromQuery] ServiceType serviceType, CancellationToken ct)
    {
        if (weightKg <= 0)
            return BadRequest ("Parcel weight must be greater than zero,");

        if (!Enum.IsDefined(serviceType))
            return BadRequest("Invalid service type. Use Standard or Express.");

        var fee = await pricing.EstimateAsync(id, weightKg, serviceType, ct);
        return fee is null ? NotFound() : Ok(fee);
    }
}