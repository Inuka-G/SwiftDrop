
using FluentAssertions;
using Moq;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Realtime.Business.DTOs;
using SwiftDrop.Realtime.Business.Services;

namespace SwiftDrop.Realtime.UnitTests;

public class ParcelStatusChangedHandlerTests
{
    private readonly Mock<ITrackingNotifier> _trackingNotifierMock;
    private readonly ParcelStatusChangedHandler _handler;

    public ParcelStatusChangedHandlerTests()
    {
        _trackingNotifierMock = new Mock<ITrackingNotifier>();

        _handler = new ParcelStatusChangedHandler(
            _trackingNotifierMock.Object
        );
    }

    [Fact]
    public async Task HandleAsync_ShouldSendCorrectDeliveryStatusUpdate()
    {
        // Arrange
        var occurredAt = DateTime.UtcNow;

        var integrationEvent = new ParcelStatusChangedIntegrationEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "SWD-10025",
            Guid.NewGuid(),
            null,
            "PickedUp",
            "InTransit",
            null,
            occurredAt
        );

        _trackingNotifierMock
            .Setup(x => x.NotifyStatusChangedAsync(
                It.IsAny<DeliveryStatusUpdate>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cancellationToken = CancellationToken.None;

        // Act
        await _handler.HandleAsync(
            integrationEvent,
            cancellationToken
        );

        // Assert
        _trackingNotifierMock.Verify(
            x => x.NotifyStatusChangedAsync(
                It.Is<DeliveryStatusUpdate>(update =>
                    update.ParcelId == integrationEvent.ParcelId &&
                    update.TrackingNumber == integrationEvent.TrackingNumber &&
                    update.Status == integrationEvent.NewStatus &&
                    update.StatusChangedAtUtc == occurredAt
                ),
                cancellationToken
            ),
            Times.Once
        );
    }

    [Fact]
    public async Task HandleAsync_ShouldThrowArgumentNullException_WhenEventIsNull()
    {
        // Act
        var act = async () => await _handler.HandleAsync(null!);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentNullException>();
    }
}
