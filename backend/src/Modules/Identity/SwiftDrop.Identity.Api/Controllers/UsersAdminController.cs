using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SwiftDrop.Identity.Api.Dtos;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = UserManagementPolicies.DispatcherOrAdmin)]
[Produces("application/json")]
public sealed class UsersAdminController(IUserAdministrationService users,
    IValidator<ListUsersRequest> listValidator,
    IValidator<ChangeUserStatusRequest> statusValidator,
    IValidator<ChangeUserRoleRequest> roleValidator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<UserAdministrationPage>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserAdministrationPage>> List([FromQuery] ListUsersRequest request, CancellationToken ct)
    {
        await ValidateAsync(listValidator, request, ct);
        var result = await users.ListAsync(new(request.Search, request.Role, request.IsActive, request.Page, request.PageSize), ct);
        return Ok(Resolve(result));
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType<UserAdministrationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAdministrationDto>> SetStatus(Guid id, ChangeUserStatusRequest request, CancellationToken ct)
    {
        await ValidateAsync(statusValidator, request, ct);
        var actorId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        return Ok(Resolve(await users.SetStatusAsync(actorId, id, request.IsActive!.Value, ct)));
    }

    [HttpPatch("{id:guid}/role")]
    [Authorize(Policy = UserManagementPolicies.AdminOnly)]
    [ProducesResponseType<UserAdministrationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserAdministrationDto>> ChangeRole(Guid id, ChangeUserRoleRequest request, CancellationToken ct)
    {
        await ValidateAsync(roleValidator, request, ct);
        var actorId = User.GetUserId() ?? throw new UnauthorizedException("The token has no user id.");
        return Ok(Resolve(await users.ChangeRoleAsync(actorId, id, request.Role!, ct)));
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T request, CancellationToken ct)
    {
        var validation = await validator.ValidateAsync(request, ct);
        if (!validation.IsValid) throw new BadRequestException(string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
    }

    private static T Resolve<T>(UserAdministrationResult<T> result) where T : class
    {
        if (result.IsSuccess) return result.Value!;
        throw result.Failure switch
        {
            UserAdministrationFailure.UserNotFound => new NotFoundException("User not found."),
            UserAdministrationFailure.InvalidRole => new BadRequestException("The role value is invalid."),
            UserAdministrationFailure.SelfChange => new BadRequestException("You cannot change your own role or status."),
            UserAdministrationFailure.LastActiveAdmin => new ConflictException("The last active administrator cannot be deactivated or demoted."),
            UserAdministrationFailure.MerchantRoleRestricted => new BadRequestException("Merchant roles are managed through the Merchants module."),
            _ => new InvalidOperationException("Unknown user administration failure.")
        };
    }
}
