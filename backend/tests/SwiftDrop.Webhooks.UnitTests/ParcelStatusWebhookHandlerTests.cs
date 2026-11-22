using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Webhooks.Business.Handlers;
using Xunit;

namespace SwiftDrop.Webhooks.UnitTests;

public sealed class ParcelStatusWebhookHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCompleteSuccessfully_WhenEventReceived()
    {
        // Arrange
        var logger = new Mock<ILogger<ParcelStatusWebhookHandler>>();
        var handler = new ParcelStatusWebhookHandler(logger.Object);
        var @event = new ParcelStatusChanged(
            ParcelId: Guid.NewGuid(),
            TrackingNumber: "SD-261122-A1B2C3",
            OldStatus: "Created",
            NewStatus: "InTransit",
            Note: "Courier picked up parcel",
            CustomerId: Guid.NewGuid(),
            MerchantId: Guid.NewGuid(),
            CourierId: Guid.NewGuid());

        // Act
        var act = () => handler.HandleAsync(@event, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
