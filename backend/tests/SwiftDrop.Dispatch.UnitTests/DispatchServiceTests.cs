using Microsoft.EntityFrameworkCore;
using Moq;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Dispatch.Contracts;
using SwiftDrop.Dispatch.Data.Entities;
using SwiftDrop.Dispatch.Data.Persistence;
using SwiftDrop.Dispatch.Data.Repositories;

namespace SwiftDrop.Dispatch.UnitTests;

public sealed class DispatchServiceTests
{
    private readonly Mock<IDispatchRepository> repository = new();
    private readonly Mock<ICourierEligibilityGateway> couriers = new();
    private readonly Mock<IParcelAssignmentGateway> parcels = new();
    private readonly Mock<IDispatchEventPublisher> events = new();
    private DispatchService Create() => new(repository.Object, couriers.Object, parcels.Object, events.Object);

    [Fact]
    public async Task Assign_publishes_event_after_successful_assignment()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        var parcel = new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD123", State = "UNASSIGNED" };
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>())).ReturnsAsync(parcel);
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        parcels.Setup(x => x.AssignAsync(parcelId, courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.True(result.IsSuccess);
        events.Verify(x => x.PublishAsync(It.Is<CourierAssignedEvent>(e => e.ParcelId == parcelId && e.CourierId == courierId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Assign_returns_not_found_for_unknown_parcel()
    {
        repository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((ParcelAssignment?)null);
        var result = await Create().AssignAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.ParcelNotFound, result.Failure);
    }

    [Fact]
    public async Task Assign_rejects_already_assigned_parcel()
    {
        repository.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "ASSIGNED" });
        var result = await Create().AssignAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.ParcelNotAssignable, result.Failure);
    }

    [Fact]
    public async Task Assign_rejects_ineligible_courier()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "UNASSIGNED" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.CourierNotEligible, result.Failure);
    }

    [Fact]
    public async Task Assign_returns_conflict_when_version_changed()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "UNASSIGNED" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        parcels.Setup(x => x.AssignAsync(parcelId, courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.SaveAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new DbUpdateConcurrencyException());
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.ConcurrencyConflict, result.Failure);
        events.Verify(x => x.PublishAsync(It.IsAny<CourierAssignedEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Parcel_created_consumer_is_idempotent()
    {
        var options = new DbContextOptionsBuilder<DispatchDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var db = new DispatchDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        var consumer = new ParcelCreatedConsumer(new DispatchRepository(db));
        var message = new ParcelCreatedMessage(Guid.NewGuid(), Guid.NewGuid(), "SD999", Guid.NewGuid(), DateTimeOffset.UtcNow);
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        Assert.Equal(1, await db.Assignments.CountAsync());
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }
}
