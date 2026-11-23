using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftDrop.Parcels.Business.Outbox;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.UnitTests.TestSupport;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.SharedKernel.Messaging;

namespace SwiftDrop.Parcels.UnitTests;

public sealed class ParcelAssignmentAndCreationTests : IDisposable
{
    private readonly ParcelsTestDb db = new();

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task Unassigned_created_parcel_can_be_assigned_once()
    {
        var parcel = await db.AddParcelAsync();
        var assignment = new ParcelAssignmentService(db.Repository, db.Clock);
        var courierId = Guid.NewGuid();

        (await assignment.AssignCourierAsync(parcel.Id, courierId, default)).Should().BeTrue();
        (await assignment.AssignCourierAsync(parcel.Id, Guid.NewGuid(), default)).Should().BeFalse();

        await using var check = db.NewContext();
        (await check.Parcels.SingleAsync()).CourierId.Should().Be(courierId);
    }

    [Fact]
    public async Task Unknown_parcel_cannot_be_assigned()
    {
        var assignment = new ParcelAssignmentService(db.Repository, db.Clock);
        (await assignment.AssignCourierAsync(Guid.NewGuid(), Guid.NewGuid(), default)).Should().BeFalse();
    }

    [Fact]
    public async Task Creating_a_parcel_records_first_history_row_and_created_event()
    {
        var pricing = new Mock<IPricingService>();
        pricing.Setup(p => p.EstimateAsync(It.IsAny<Guid>(), It.IsAny<decimal>(), It.IsAny<ServiceType>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeeEstimateDto(Guid.NewGuid(), "Kandy", 500m, 100m, 600m));
        var publisher = new Mock<SwiftDrop.SharedKernel.Messaging.IEventPublisher>();
        publisher
            .Setup(p => p.PublishAsync(It.IsAny<ParcelStatusChanged>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("RabbitMQ unavailable"));
        var service = new ParcelService(db.Repository, pricing.Object, publisher.Object);
        var customerId = Guid.NewGuid();

        var created = await service.CreateParcelAsync(customerId, new CreateParcelRequest(Guid.NewGuid(),
            "1 Main St, Colombo", "Sender", "+94771234567", "2 Hill St, Kandy", "Receiver", "+94779876543",
            2m, "Small"), default);

        await using var check = db.NewContext();
        var history = await check.StatusHistory.SingleAsync();
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(ParcelStatuses.Created);

        var parcel = await check.Parcels.SingleAsync(p => p.Id == created.ParcelId);
        parcel.EstimatedFee.Should().Be(600m);

        var outbox = await check.OutboxMessages.OrderBy(m => m.RoutingKey).ToListAsync();
        outbox.Should().HaveCount(2);
        var createdMessage = outbox.Single(m => m.RoutingKey == ParcelEventRoutingKeys.Created);
        var createdEvent = JsonSerializer.Deserialize<ParcelCreatedIntegrationEvent>(createdMessage.Payload, JsonSerializerOptions.Web)!;
        createdEvent.ParcelId.Should().Be(created.ParcelId);
        createdEvent.CustomerId.Should().Be(customerId);
        createdEvent.TrackingNumber.Should().Be(created.TrackingNumber);

        var statusMessage = outbox.Single(m => m.RoutingKey == "parcel.status.changed");
        var statusEvent = JsonSerializer.Deserialize<ParcelStatusChanged>(statusMessage.Payload, JsonSerializerOptions.Web)!;
        statusMessage.Id.Should().Be(statusEvent.EventId);
        statusEvent.ParcelId.Should().Be(created.ParcelId);
        statusEvent.TrackingNumber.Should().Be(created.TrackingNumber);
        statusEvent.OldStatus.Should().Be("None");
        statusEvent.NewStatus.Should().Be("Created");
        statusEvent.Note.Should().Be("Parcel created.");
        statusEvent.CustomerId.Should().Be(customerId);

        publisher.Verify(p => p.PublishAsync(
            It.IsAny<ParcelStatusChanged>(), It.IsAny<CancellationToken>()), Times.Never);

        var outboxPublisher = new Mock<IIntegrationEventPublisher>();
        outboxPublisher
            .Setup(p => p.PublishAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await using var outboxContext = db.NewContext();
        var dispatcher = new OutboxDispatcher(
            new OutboxRepository(outboxContext),
            outboxPublisher.Object,
            db.Clock,
            Options.Create(new OutboxOptions()),
            NullLogger<OutboxDispatcher>.Instance);

        (await dispatcher.DispatchPendingAsync(default)).Should().Be(2);
        outboxPublisher.Verify(p => p.PublishAsync(
            statusMessage.Id,
            "parcel.status.changed",
            It.Is<string>(payload => payload.Contains(created.TrackingNumber)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Creating_a_merchant_parcel_uses_the_requested_express_pricing()
    {
        var pricing = new Mock<IPricingService>();
        var zoneId = Guid.NewGuid();
        pricing.Setup(p => p.EstimateAsync(zoneId, 2m, ServiceType.Express, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeeEstimateDto(zoneId, "Kandy", 600m, 150m, 750m));
        var service = new ParcelService(db.Repository, pricing.Object, Mock.Of<IEventPublisher>());
        var merchantId = Guid.NewGuid();
        var request = new CreateParcelRequest(zoneId, "Pickup", "Sender", "+94771234567",
            "Dropoff", "Receiver", "+94779876543", 2m, "Small", ServiceType: ServiceType.Express);

        var created = await service.CreateMerchantParcelAsync(merchantId, request, default);

        created.EstimatedFee.Should().Be(750m);
        pricing.Verify(p => p.EstimateAsync(zoneId, 2m, ServiceType.Express, It.IsAny<CancellationToken>()), Times.Once);
        await using var check = db.NewContext();
        var parcel = await check.Parcels.SingleAsync(p => p.Id == created.ParcelId);
        parcel.MerchantId.Should().Be(merchantId);
        parcel.CustomerId.Should().BeNull();
        parcel.EstimatedFee.Should().Be(750m);
    }
}
