using Microsoft.Extensions.Logging;
using SwiftDrop.Identity.Contracts;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.Identity.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Business.Services;

internal sealed class UserAdministrationService(IUserRepository users, IIdentityEventPublisher events,
    ILogger<UserAdministrationService> logger) : IUserAdministrationService
{
    private static readonly string[] SupportedRoles =
        [Roles.Customer, Roles.Merchant, Roles.Courier, Roles.Dispatcher, Roles.Admin];

    public async Task<UserAdministrationResult<UserAdministrationPage>> ListAsync(UserAdministrationQuery query, CancellationToken ct)
    {
        var role = NormalizeRole(query.Role);
        if (query.Role is not null && (role is null || !SupportedRoles.Contains(role, StringComparer.Ordinal)))
            return UserAdministrationResult<UserAdministrationPage>.Fail(UserAdministrationFailure.InvalidRole);

        var (items, total) = await users.ListUsersAsync(query.Search, role, query.IsActive, query.Page, query.PageSize, ct);
        var response = new UserAdministrationPage(items.Select(ToDto).ToArray(), query.Page, query.PageSize, total);
        return UserAdministrationResult<UserAdministrationPage>.Success(response);
    }

    public async Task<UserAdministrationResult<UserAdministrationDto>> SetStatusAsync(Guid actorId, Guid userId,
        bool isActive, CancellationToken ct)
    {
        var changedAt = DateTimeOffset.UtcNow;
        var result = await users.ExecuteInTransactionAsync(async token =>
        {
            var user = await users.GetByIdAsync(userId, token);
            if (user is null) return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.UserNotFound), (UserStatusChanged?)null);
            if (user.IsActive == isActive) return (UserAdministrationResult<UserAdministrationDto>.Success(ToDto(user)), (UserStatusChanged?)null);
            if (actorId == userId) return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.SelfChange), (UserStatusChanged?)null);
            if (!isActive && user.Role.Name == Roles.Admin && await users.CountActiveUsersInRoleAsync(Roles.Admin, token) <= 1)
                return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.LastActiveAdmin), (UserStatusChanged?)null);

            if (isActive) user.Activate(); else user.Deactivate();
            await users.SaveChangesAsync(token);
            var eventAt = DateTimeOffset.UtcNow;
            return (UserAdministrationResult<UserAdministrationDto>.Success(ToDto(user)),
                new UserStatusChanged(Guid.NewGuid(), userId, isActive, eventAt));
        }, ct);

        if (result.Item2 is not null)
        {
            logger.LogInformation("User {TargetUserId} status changed by {ActorUserId}.", userId, actorId);
            await PublishAfterSaveAsync(() => events.PublishAsync(result.Item2, ct), actorId, userId, "status", ct);
        }
        return result.Item1;
    }

    public async Task<UserAdministrationResult<UserAdministrationDto>> ChangeRoleAsync(Guid actorId, Guid userId,
        string roleName, CancellationToken ct)
    {
        var newRoleName = NormalizeRole(roleName);
        if (newRoleName is null || !SupportedRoles.Contains(newRoleName, StringComparer.Ordinal))
            return UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.InvalidRole);

        var result = await users.ExecuteInTransactionAsync(async token =>
        {
            var user = await users.GetByIdAsync(userId, token);
            if (user is null) return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.UserNotFound), (UserRoleChanged?)null);
            if (user.Role.Name == newRoleName) return (UserAdministrationResult<UserAdministrationDto>.Success(ToDto(user)), (UserRoleChanged?)null);
            if (actorId == userId) return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.SelfChange), (UserRoleChanged?)null);
            if (user.Role.Name == Roles.Merchant || newRoleName == Roles.Merchant)
                return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.MerchantRoleRestricted), (UserRoleChanged?)null);
            if (user.IsActive && user.Role.Name == Roles.Admin && newRoleName != Roles.Admin &&
                await users.CountActiveUsersInRoleAsync(Roles.Admin, token) <= 1)
                return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.LastActiveAdmin), (UserRoleChanged?)null);

            var role = await users.GetRoleAsync(newRoleName, token);
            if (role is null) return (UserAdministrationResult<UserAdministrationDto>.Fail(UserAdministrationFailure.InvalidRole), (UserRoleChanged?)null);
            var previousRole = user.Role.Name;
            user.ChangeRole(role);
            await users.SaveChangesAsync(token);
            return (UserAdministrationResult<UserAdministrationDto>.Success(ToDto(user)),
                new UserRoleChanged(Guid.NewGuid(), userId, previousRole, newRoleName, DateTimeOffset.UtcNow));
        }, ct);

        if (result.Item2 is not null)
        {
            logger.LogInformation("User {TargetUserId} role changed by {ActorUserId}.", userId, actorId);
            await PublishAfterSaveAsync(() => events.PublishAsync(result.Item2, ct), actorId, userId, "role", ct);
        }
        return result.Item1;
    }

    private async Task PublishAfterSaveAsync(Func<Task> publish, Guid actorId, Guid targetId, string action, CancellationToken ct)
    {
        try { await publish(); }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // No outbox is present; the committed user change remains successful if the broker is unavailable.
            logger.LogWarning("User {TargetUserId} {Action} event could not be published after update by {ActorUserId}.", targetId, action, actorId);
        }
    }

    private static string? NormalizeRole(string? role) => string.IsNullOrWhiteSpace(role) ? null : role.Trim().ToUpperInvariant();
    private static UserAdministrationDto ToDto(User user) => new(user.Id, user.FullName, user.Email,
        user.Role.Name, user.IsActive, user.AuthProvider, user.CreatedAtUtc);
}
