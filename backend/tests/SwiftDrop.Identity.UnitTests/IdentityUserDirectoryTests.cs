using SwiftDrop.Identity.Business.Services;
using SwiftDrop.Identity.Data.Entities;

namespace SwiftDrop.Identity.UnitTests;

public sealed class IdentityUserDirectoryTests
{
    [Fact]
    public async Task Active_courier_is_eligible_for_dispatch_registration()
    {
        var users = new FakeUserRepository();
        var courier = new User("Demo Courier", "courier@test.local", "courier", null, FakeUserRepository.CourierRole);
        users.Users.Add(courier);

        var result = await new IdentityUserDirectory(users).IsActiveCourierAsync(courier.Id, default);

        Assert.True(result);
    }

    [Fact]
    public async Task Inactive_or_non_courier_accounts_are_not_eligible()
    {
        var users = new FakeUserRepository();
        var inactiveCourier = new User("Inactive Courier", "inactive@test.local", "inactive", null, FakeUserRepository.CourierRole);
        inactiveCourier.Deactivate();
        var customer = new User("Customer", "customer@test.local", "customer", null, FakeUserRepository.CustomerRole);
        users.Users.AddRange([inactiveCourier, customer]);
        var directory = new IdentityUserDirectory(users);

        Assert.False(await directory.IsActiveCourierAsync(inactiveCourier.Id, default));
        Assert.False(await directory.IsActiveCourierAsync(customer.Id, default));
        Assert.False(await directory.IsActiveCourierAsync(Guid.NewGuid(), default));
    }

    [Fact]
    public async Task Name_lookup_returns_only_requested_users()
    {
        var users = new FakeUserRepository();
        var courier = new User("Demo Courier", "courier@test.local", "courier", null, FakeUserRepository.CourierRole);
        users.Users.Add(courier);

        var names = await new IdentityUserDirectory(users).GetNamesAsync([courier.Id, Guid.NewGuid()], default);

        Assert.Equal("Demo Courier", names[courier.Id]);
        Assert.Single(names);
    }
}
