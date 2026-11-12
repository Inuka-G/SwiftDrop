using System.Text.Json;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftDrop.Identity.Api.Dtos;
using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Contracts;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.UnitTests;

public sealed class UserAdministrationTests
{
    private readonly FakeUserRepository repository = new();
    private readonly FakeIdentityEventPublisher publisher;
    private readonly UserAdministrationService service;
    private static readonly Guid AdminId = Guid.NewGuid();

    public UserAdministrationTests()
    {
        publisher = new FakeIdentityEventPublisher(() => repository.SaveCount);
        service = new UserAdministrationService(repository, publisher, NullLogger<UserAdministrationService>.Instance);
    }

    [Fact]
    public async Task Status_change_updates_user_and_publishes_once_after_save()
    {
        var target = AddUser("customer", FakeUserRepository.CustomerRole);

        var result = await service.SetStatusAsync(AdminId, target.Id, false, default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsActive);
        var message = Assert.Single(publisher.StatusEvents);
        Assert.Equal(target.Id, message.UserId);
        Assert.True(publisher.SawSavedStateWhenPublished);
    }

    [Fact]
    public async Task No_op_status_and_role_changes_do_not_publish()
    {
        var target = AddUser("customer", FakeUserRepository.CustomerRole);

        Assert.True((await service.SetStatusAsync(AdminId, target.Id, true, default)).IsSuccess);
        Assert.True((await service.ChangeRoleAsync(AdminId, target.Id, Roles.Customer, default)).IsSuccess);
        Assert.Empty(publisher.StatusEvents);
        Assert.Empty(publisher.RoleEvents);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task User_cannot_change_own_status_or_role()
    {
        var admin = AddUser("admin", FakeUserRepository.AdminRole);

        Assert.Equal(UserAdministrationFailure.SelfChange,
            (await service.SetStatusAsync(admin.Id, admin.Id, false, default)).Failure);
        Assert.Equal(UserAdministrationFailure.SelfChange,
            (await service.ChangeRoleAsync(admin.Id, admin.Id, Roles.Customer, default)).Failure);
        Assert.Empty(publisher.StatusEvents);
        Assert.Empty(publisher.RoleEvents);
    }

    [Fact]
    public async Task Unknown_user_and_role_return_failures_without_throwing()
    {
        Assert.Equal(UserAdministrationFailure.UserNotFound,
            (await service.SetStatusAsync(AdminId, Guid.NewGuid(), false, default)).Failure);
        Assert.Equal(UserAdministrationFailure.InvalidRole,
            (await service.ChangeRoleAsync(AdminId, Guid.NewGuid(), "NOT_A_ROLE", default)).Failure);
        Assert.Empty(publisher.StatusEvents);
        Assert.Empty(publisher.RoleEvents);
    }

    [Fact]
    public async Task Merchant_roles_cannot_be_changed_in_either_direction()
    {
        var customer = AddUser("customer", FakeUserRepository.CustomerRole);
        var merchant = AddUser("merchant", FakeUserRepository.MerchantRole);

        Assert.Equal(UserAdministrationFailure.MerchantRoleRestricted,
            (await service.ChangeRoleAsync(AdminId, customer.Id, Roles.Merchant, default)).Failure);
        Assert.Equal(UserAdministrationFailure.MerchantRoleRestricted,
            (await service.ChangeRoleAsync(AdminId, merchant.Id, Roles.Courier, default)).Failure);
        Assert.Empty(publisher.RoleEvents);
    }

    [Fact]
    public async Task Last_active_admin_cannot_be_deactivated_or_demoted()
    {
        var admin = AddUser("admin", FakeUserRepository.AdminRole);

        Assert.Equal(UserAdministrationFailure.LastActiveAdmin,
            (await service.SetStatusAsync(AdminId, admin.Id, false, default)).Failure);
        Assert.Equal(UserAdministrationFailure.LastActiveAdmin,
            (await service.ChangeRoleAsync(AdminId, admin.Id, Roles.Customer, default)).Failure);
        Assert.True(admin.IsActive);
        Assert.Empty(publisher.StatusEvents);
        Assert.Empty(publisher.RoleEvents);
    }

    [Fact]
    public async Task Another_active_admin_can_be_demoted_when_more_than_one_exists()
    {
        var first = AddUser("admin1", FakeUserRepository.AdminRole);
        AddUser("admin2", FakeUserRepository.AdminRole);

        var result = await service.ChangeRoleAsync(AdminId, first.Id, Roles.Customer, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Roles.Customer, result.Value!.Role);
        Assert.Equal(Roles.Admin, Assert.Single(publisher.RoleEvents).PreviousRole);
    }

    [Fact]
    public async Task Listing_applies_role_status_search_and_paging_and_dto_has_no_secrets()
    {
        var matching = AddUser("percent", FakeUserRepository.CustomerRole, "Test % Person", "matching@example.test");
        matching.Deactivate();
        AddUser("another", FakeUserRepository.CustomerRole, "Test Person", "other@example.test");
        AddUser("courier", FakeUserRepository.CourierRole, "Test % Person", "courier@example.test");

        var result = await service.ListAsync(new("%", Roles.Customer, false, 1, 10), default);

        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value!.Items);
        Assert.Equal(matching.Id, dto.Id);
        Assert.Equal(1, result.Value.TotalCount);
        var json = JsonSerializer.Serialize(dto);
        Assert.DoesNotContain("password", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerUserId", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("phoneNumber", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validators_reject_invalid_paging_and_roles()
    {
        var list = new ListUsersRequestValidator();
        Assert.False(list.TestValidate(new ListUsersRequest(Page: 0)).IsValid);
        Assert.False(list.TestValidate(new ListUsersRequest(PageSize: 101)).IsValid);
        Assert.False(list.TestValidate(new ListUsersRequest(Role: "UNKNOWN")).IsValid);
        Assert.False(new ChangeUserStatusRequestValidator().TestValidate(new ChangeUserStatusRequest(null)).IsValid);
        Assert.False(new ChangeUserRoleRequestValidator().TestValidate(new ChangeUserRoleRequest("UNKNOWN")).IsValid);
    }

    [Fact]
    public void Validators_accept_supported_filters_and_changes()
    {
        Assert.True(new ListUsersRequestValidator().TestValidate(new ListUsersRequest("test", "courier", true, 1, 100)).IsValid);
        Assert.True(new ChangeUserStatusRequestValidator().TestValidate(new ChangeUserStatusRequest(false)).IsValid);
        Assert.True(new ChangeUserRoleRequestValidator().TestValidate(new ChangeUserRoleRequest(Roles.Dispatcher)).IsValid);
    }

    private User AddUser(string username, Role role, string? name = null, string? email = null)
    {
        var user = new User(name ?? username, email ?? $"{username}@test.local", username, "+123456", role);
        repository.Users.Add(user);
        return user;
    }

    private sealed class FakeIdentityEventPublisher(Func<int> saveCount) : IIdentityEventPublisher
    {
        public List<UserRoleChanged> RoleEvents { get; } = [];
        public List<UserStatusChanged> StatusEvents { get; } = [];
        public bool SawSavedStateWhenPublished { get; private set; }

        public Task PublishAsync(UserRoleChanged message, CancellationToken ct)
        {
            SawSavedStateWhenPublished = saveCount() > 0;
            RoleEvents.Add(message);
            return Task.CompletedTask;
        }

        public Task PublishAsync(UserStatusChanged message, CancellationToken ct)
        {
            SawSavedStateWhenPublished = saveCount() > 0;
            StatusEvents.Add(message);
            return Task.CompletedTask;
        }
    }
}
