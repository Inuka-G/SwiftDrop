using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Moq;
using SwiftDrop.Realtime.Api.Hubs;

namespace SwiftDrop.Realtime.UnitTests;

public sealed class TrackingHubTests
{
    [Fact]
    public async Task SubscribeToTracking_AddsConnectionToNormalizedTrackingGroup()
    {
        var groups = new Mock<IGroupManager>();
        groups.Setup(x => x.AddToGroupAsync("connection-1", "tracking-SWD-10025", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var hub = CreateHub(groups.Object);

        await hub.SubscribeToTracking("  swd-10025  ");

        groups.Verify(x => x.AddToGroupAsync("connection-1", "tracking-SWD-10025", CancellationToken.None), Times.Once);
        groups.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnsubscribeFromTracking_RemovesConnectionFromNormalizedTrackingGroup()
    {
        var groups = new Mock<IGroupManager>();
        groups.Setup(x => x.RemoveFromGroupAsync("connection-1", "tracking-SWD-10025", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var hub = CreateHub(groups.Object);

        await hub.UnsubscribeFromTracking("  swd-10025  ");

        groups.Verify(x => x.RemoveFromGroupAsync("connection-1", "tracking-SWD-10025", CancellationToken.None), Times.Once);
        groups.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task SubscribeToTracking_RejectsMissingTrackingNumber(string? trackingNumber)
    {
        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        var hub = CreateHub(groups.Object);

        var act = () => hub.SubscribeToTracking(trackingNumber!);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("Tracking number is required.");
        groups.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task UnsubscribeFromTracking_RejectsMissingTrackingNumber(string? trackingNumber)
    {
        var groups = new Mock<IGroupManager>(MockBehavior.Strict);
        var hub = CreateHub(groups.Object);

        var act = () => hub.UnsubscribeFromTracking(trackingNumber!);

        await act.Should().ThrowAsync<HubException>()
            .WithMessage("Tracking number is required.");
        groups.VerifyNoOtherCalls();
    }

    private static TrackingHub CreateHub(IGroupManager groups)
    {
        var context = new Mock<HubCallerContext>();
        context.SetupGet(x => x.ConnectionId).Returns("connection-1");

        return new TrackingHub
        {
            Context = context.Object,
            Groups = groups
        };
    }
}
