using FluentValidation;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Api.Dtos;

public sealed record ListUsersRequest(string? Search = null, string? Role = null, bool? IsActive = null,
    int Page = 1, int PageSize = UserAdministrationLimits.DefaultPageSize);

public sealed record ChangeUserStatusRequest(bool? IsActive);
public sealed record ChangeUserRoleRequest(string? Role);

internal sealed class ListUsersRequestValidator : AbstractValidator<ListUsersRequest>
{
    private static readonly string[] Roles = [SwiftDrop.SharedKernel.Authorization.Roles.Customer,
        SwiftDrop.SharedKernel.Authorization.Roles.Merchant, SwiftDrop.SharedKernel.Authorization.Roles.Courier,
        SwiftDrop.SharedKernel.Authorization.Roles.Dispatcher, SwiftDrop.SharedKernel.Authorization.Roles.Admin];

    public ListUsersRequestValidator()
    {
        RuleFor(x => x.Search).MaximumLength(UserAdministrationLimits.MaximumSearchLength);
        RuleFor(x => x.Role).Must(role => string.IsNullOrWhiteSpace(role) || Roles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
            .WithMessage("The role filter is invalid.");
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, UserAdministrationLimits.MaximumPageSize);
    }
}

internal sealed class ChangeUserStatusRequestValidator : AbstractValidator<ChangeUserStatusRequest>
{
    public ChangeUserStatusRequestValidator() => RuleFor(x => x.IsActive).NotNull();
}

internal sealed class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequest>
{
    private static readonly string[] Roles = [SwiftDrop.SharedKernel.Authorization.Roles.Customer,
        SwiftDrop.SharedKernel.Authorization.Roles.Merchant, SwiftDrop.SharedKernel.Authorization.Roles.Courier,
        SwiftDrop.SharedKernel.Authorization.Roles.Dispatcher, SwiftDrop.SharedKernel.Authorization.Roles.Admin];

    public ChangeUserRoleRequestValidator() => RuleFor(x => x.Role)
        .Must(role => !string.IsNullOrWhiteSpace(role) && Roles.Contains(role.Trim(), StringComparer.OrdinalIgnoreCase))
        .WithMessage("A valid role is required.");
}
