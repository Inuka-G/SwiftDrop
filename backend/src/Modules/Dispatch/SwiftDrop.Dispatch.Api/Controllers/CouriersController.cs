using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Dispatch.Api.Dtos;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Dispatch.Api.Controllers;

[ApiController]
[Route("api/v1/dispatch/couriers")]
[Authorize(Roles = Roles.DispatcherOrAdmin)]
[Produces("application/json")]
public sealed class CouriersController(IDispatchService dispatch,
    IValidator<RegisterCourierRequest> registerValidator,
    IValidator<UpdateCourierRequest> updateValidator,
    IValidator<ListCouriersRequest> listValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CourierProfilePage>> List([FromQuery] ListCouriersRequest request, CancellationToken ct)
    {
        await ValidateAsync(listValidator, request, ct);
        return Ok(await dispatch.ListCouriersAsync(request.ZoneId, request.IsAvailable, request.Page, request.PageSize, ct));
    }

    [HttpGet("summary")]
    public async Task<ActionResult<CourierDashboardSummary>> Summary(CancellationToken ct) =>
        Ok(await dispatch.GetCourierSummaryAsync(ct));

    [HttpPost]
    public async Task<ActionResult<CourierProfileDto>> Register(RegisterCourierRequest request, CancellationToken ct)
    {
        await ValidateAsync(registerValidator, request, ct);
        var result = await dispatch.RegisterCourierAsync(request.CourierId, request.ZoneId,
            request.VehicleType, request.LicensePlate, ct);
        if (!result.IsSuccess) throw ToException(result.Failure!.Value);
        return CreatedAtAction(nameof(List), new { }, result.Profile);
    }

    [HttpPatch("{courierId:guid}")]
    public async Task<ActionResult<CourierProfileDto>> Update(Guid courierId, UpdateCourierRequest request, CancellationToken ct)
    {
        await ValidateAsync(updateValidator, request, ct);
        var result = await dispatch.UpdateCourierAsync(courierId, request.ZoneId, request.IsAvailable,
            request.VehicleType, request.LicensePlate, ct);
        if (!result.IsSuccess) throw ToException(result.Failure!.Value);
        return Ok(result.Profile);
    }

    private static Exception ToException(DispatchFailure failure) => failure switch
    {
        DispatchFailure.CourierAlreadyRegistered => new ConflictException("Courier profile already exists."),
        DispatchFailure.CourierProfileNotFound => new NotFoundException("Courier profile was not found."),
        DispatchFailure.CourierNotEligible => new ForbiddenException("User is not an active courier."),
        DispatchFailure.ZoneNotFound => new NotFoundException("Zone was not found."),
        DispatchFailure.ConcurrencyConflict => new ConflictException("Courier profile changed concurrently. Reload and try again."),
        _ => new BadRequestException("Courier profile request is invalid.")
    };

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid) throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
