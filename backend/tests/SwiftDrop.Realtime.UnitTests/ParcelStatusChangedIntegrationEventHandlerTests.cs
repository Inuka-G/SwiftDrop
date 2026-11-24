using FluentAssertions;
using Moq;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Realtime.Business.DTOs;
using SwiftDrop.Realtime.Business.Services;

namespace SwiftDrop.Realtime.UnitTests;

public sealed class ParcelStatusChangedIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_MapsEventAndNotifiesTrackingClients()
    {
        var notifier = new Mock<ITrackingNotifier>();
        var handler = new ParcelStatusChangedIntegrationEventHandler(notifier.Object);
        var parcelId = Guid.NewGuid();
        var occurredAtUtc = DateTime.UtcNow;
        var integrationEvent = new ParcelStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            parcelId,
            "SD-TRACK-123",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "InTransit",
            "OutForDelivery",
            null,
            occurredAtUtc);
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;

        notifier
            .Setup(x => x.NotifyStatusChangedAsync(
                It.IsAny<DeliveryStatusUpdate>(),
                cancellationToken))
            .Returns(Task.CompletedTask);

        await handler.HandleAsync(integrationEvent, cancellationToken);

        notifier.Verify(x => x.NotifyStatusChangedAsync(
            It.Is<DeliveryStatusUpdate>(update =>
                update.ParcelId == parcelId &&
                update.TrackingNumber == integrationEvent.TrackingNumber &&
                update.Status == integrationEvent.NewStatus &&
                update.StatusChangedAtUtc == occurredAtUtc),
            cancellationToken), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ThrowsWhenEventIsNull()
    {
        var notifier = new Mock<ITrackingNotifier>();
        var handler = new ParcelStatusChangedIntegrationEventHandler(notifier.Object);

        var act = () => handler.HandleAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
        notifier.VerifyNoOtherCalls();
    }
}
