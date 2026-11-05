using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Dispatch.Api.Dtos;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Dispatch.Api.Controllers;

[ApiController]
[Route("api/v1/dispatch/parcels")]
[Authorize(Roles = Roles.DispatcherOrAdmin)]
[Produces("application/json")]
public sealed class DispatchController(IDispatchService dispatch, IValidator<ListUnassignedRequest> listValidator,
    IValidator<AssignCourierRequest> assignValidator) : ControllerBase
{
    [HttpGet("unassigned")]
    public async Task<ActionResult<UnassignedParcelPage>> List([FromQuery] ListUnassignedRequest request, CancellationToken ct)
    {
        await ValidateAsync(listValidator, request, ct);
        return Ok(await dispatch.ListUnassignedAsync(request.Zone, request.From, request.To, request.Page, request.PageSize, ct));
    }

    [HttpPost("{parcelId:guid}/assign")]
    public async Task<ActionResult<AssignmentDto>> Assign(Guid parcelId, AssignCourierRequest request, CancellationToken ct)
    {
        await ValidateAsync(assignValidator, request, ct);
        var assignedBy = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        var result = await dispatch.AssignAsync(parcelId, request.CourierId, assignedBy, ct);
        if (result.IsSuccess) return Ok(result.Value);
        throw result.Failure switch
        {
            DispatchFailure.ParcelNotFound => new NotFoundException("Parcel was not found."),
            DispatchFailure.ParcelNotAssignable => new ConflictException("Parcel is not available for assignment."),
            DispatchFailure.CourierNotEligible => new ForbiddenException("Courier is not eligible for assignment."),
            DispatchFailure.ConcurrencyConflict => new ConflictException("Parcel assignment changed concurrently. Reload and try again."),
            _ => new InvalidOperationException("Unknown dispatch failure.")
        };
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(request, ct);
        if (!result.IsValid) throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.ErrorMessage)));
    }
}
