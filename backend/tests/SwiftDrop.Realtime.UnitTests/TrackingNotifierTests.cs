using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SwiftDrop.Realtime.Api.Hubs;
using SwiftDrop.Realtime.Api.Services;
using SwiftDrop.Realtime.Business.DTOs;

namespace SwiftDrop.Realtime.UnitTests;

public sealed class TrackingNotifierTests
{
    [Fact]
    public async Task NotifyStatusChangedAsync_SendsEventToNormalizedGroupWithCorrectPayload()
    {
        const string normalizedGroup = "tracking-SWD-30075";
        var update = new DeliveryStatusUpdate(
            Guid.NewGuid(),
            " swd-30075 ",
            "OutForDelivery",
            DateTime.UtcNow);
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        var clientProxy = new Mock<IClientProxy>();
        clientProxy
            .Setup(x => x.SendCoreAsync(
                "DeliveryStatusUpdated",
                It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], update)),
                cancellationToken))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubClients>();
        clients.Setup(x => x.Group(normalizedGroup)).Returns(clientProxy.Object);

        var hubContext = new Mock<IHubContext<TrackingHub>>();
        hubContext.SetupGet(x => x.Clients).Returns(clients.Object);

        var notifier = new TrackingNotifier(hubContext.Object);

        await notifier.NotifyStatusChangedAsync(update, cancellationToken);

        clients.Verify(x => x.Group(normalizedGroup), Times.Once);
        clientProxy.Verify(x => x.SendCoreAsync(
            "DeliveryStatusUpdated",
            It.Is<object?[]>(args => args.Length == 1 && Equals(args[0], update)),
            cancellationToken), Times.Once);
    }
}
