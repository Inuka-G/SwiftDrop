using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.UnitTests.TestSupport;
using SwiftDrop.Pricing.Contracts;

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
        var service = new ParcelService(db.Repository, pricing.Object);
        var customerId = Guid.NewGuid();

        var created = await service.CreateParcelAsync(customerId, new CreateParcelRequest(Guid.NewGuid(),
            "1 Main St, Colombo", "Sender", "+94771234567", "2 Hill St, Kandy", "Receiver", "+94779876543",
            2m, "Small"), default);

        await using var check = db.NewContext();
        var history = await check.StatusHistory.SingleAsync();
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(ParcelStatuses.Created);

        var outbox = await check.OutboxMessages.SingleAsync();
        outbox.RoutingKey.Should().Be(ParcelEventRoutingKeys.Created);
        var evt = JsonSerializer.Deserialize<ParcelCreatedIntegrationEvent>(outbox.Payload, JsonSerializerOptions.Web)!;
        evt.ParcelId.Should().Be(created.ParcelId);
        evt.CustomerId.Should().Be(customerId);
        evt.TrackingNumber.Should().Be(created.TrackingNumber);
    }
}
