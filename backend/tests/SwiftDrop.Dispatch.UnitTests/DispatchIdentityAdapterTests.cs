using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SwiftDrop.Dispatch.Business;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Identity.Contracts;

namespace SwiftDrop.Dispatch.UnitTests;

public sealed class DispatchIdentityAdapterTests
{
    [Fact]
    public async Task Dispatch_registration_resolves_identity_adapter_and_delegates_courier_lookup()
    {
        var courierId = Guid.NewGuid();
        var directory = new Mock<IIdentityUserDirectory>();
        directory.Setup(x => x.IsActiveCourierAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        directory.Setup(x => x.GetNamesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [courierId] = "Demo Courier" });

        var services = new ServiceCollection();
        services.AddSingleton(directory.Object);
        services.AddDispatchBusiness(new ConfigurationBuilder().Build());
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var gateway = scope.ServiceProvider.GetRequiredService<ICourierEligibilityGateway>();

        Assert.True(await gateway.IsActiveCourierAsync(courierId, default));
        Assert.Equal("Demo Courier", (await gateway.GetNamesAsync([courierId], default))[courierId]);
        directory.Verify(x => x.IsActiveCourierAsync(courierId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
