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
        var service = new ParcelService(db.Repository, pricing.Object, db.Clock);
        var customerId = Guid.NewGuid();

        var created = await service.CreateParcelAsync(customerId, new CreateParcelRequest(Guid.NewGuid(),
            "1 Main St, Colombo", "Sender", "+94771234567", "2 Hill St, Kandy", "Receiver", "+94779876543",
            2m, "Small"), default);

        await using var check = db.NewContext();
        var history = await check.StatusHistory.SingleAsync();
        history.FromStatus.Should().BeNull();
        history.ToStatus.Should().Be(ParcelStatuses.Created);

        // Both events are in the outbox (same transaction as the parcel), nothing was sent directly
        var outbox = await check.OutboxMessages.ToListAsync();
        outbox.Select(m => m.RoutingKey).Should().BeEquivalentTo(["parcel.created", "parcel.status.changed"]);

        var createdEvt = JsonSerializer.Deserialize<ParcelCreated>(
            outbox.Single(m => m.RoutingKey == "parcel.created").Payload, JsonSerializerOptions.Web)!;
        createdEvt.ParcelId.Should().Be(created.ParcelId);
        createdEvt.CustomerId.Should().Be(customerId);
        createdEvt.TrackingNumber.Should().Be(created.TrackingNumber);

        var statusEvt = JsonSerializer.Deserialize<ParcelStatusChanged>(
            outbox.Single(m => m.RoutingKey == "parcel.status.changed").Payload, JsonSerializerOptions.Web)!;
        statusEvt.OldStatus.Should().Be("None");
        statusEvt.NewStatus.Should().Be(ParcelStatuses.Created);
    }

    [Fact]
    public async Task Admin_status_change_uses_the_lifecycle_rules()
    {
        var parcel = await db.AddParcelAsync();
        var service = new ParcelService(db.Repository, Mock.Of<IPricingService>(), db.Clock);
        var adminId = Guid.NewGuid();

        var skip = () => service.UpdateStatusAsync(parcel.Id, ParcelStatuses.Delivered, null, adminId, "ADMIN", default);
        await skip.Should().ThrowAsync<SwiftDrop.SharedKernel.Exceptions.ConflictException>();

        (await service.UpdateStatusAsync(parcel.Id, ParcelStatuses.PickedUp, "  At depot  ", adminId, "ADMIN", default))
            .Should().BeTrue();
        (await service.UpdateStatusAsync(Guid.NewGuid(), ParcelStatuses.PickedUp, null, adminId, "ADMIN", default))
            .Should().BeFalse();

        await using var check = db.NewContext();
        var history = await check.StatusHistory.SingleAsync();
        history.Note.Should().Be("At depot");
        history.ChangedByRole.Should().Be("ADMIN");
        (await check.OutboxMessages.SingleAsync()).RoutingKey.Should().Be("parcel.status.changed");
    }
}
