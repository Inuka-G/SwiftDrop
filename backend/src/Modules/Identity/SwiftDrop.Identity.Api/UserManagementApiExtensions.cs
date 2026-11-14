using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SwiftDrop.Identity.Api.Dtos;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Api;

internal static class UserManagementApiExtensions
{
    public static IServiceCollection AddUserManagementApi(this IServiceCollection services)
    {
        services.AddScoped<IValidator<ListUsersRequest>, ListUsersRequestValidator>();
        services.AddScoped<IValidator<ChangeUserStatusRequest>, ChangeUserStatusRequestValidator>();
        services.AddScoped<IValidator<ChangeUserRoleRequest>, ChangeUserRoleRequestValidator>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(UserManagementPolicies.DispatcherOrAdmin,
                policy => policy.RequireRole(Roles.Dispatcher, Roles.Admin));
            options.AddPolicy(UserManagementPolicies.AdminOnly,
                policy => policy.RequireRole(Roles.Admin));
        });
        return services;
    }
}
