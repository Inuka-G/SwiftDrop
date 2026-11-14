using Microsoft.EntityFrameworkCore;
using Moq;
using SwiftDrop.Dispatch.Business.Consumers;
using SwiftDrop.Dispatch.Business.Services;
using SwiftDrop.Dispatch.Api.Dtos;
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
    private readonly Mock<IZoneLookup> zones = new();
    private DispatchService Create() => new(repository.Object, couriers.Object, parcels.Object, events.Object, zones.Object);

    [Fact]
    public async Task Assign_publishes_event_after_successful_assignment()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        var parcel = new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD123", State = "UNASSIGNED" };
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId, IsAvailable = true });
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
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId, IsAvailable = true });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.CourierNotEligible, result.Failure);
    }

    [Fact]
    public async Task Assign_rejects_unavailable_courier_profile()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "UNASSIGNED" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId, IsAvailable = false });
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.CourierNotEligible, result.Failure);
    }

    [Fact]
    public async Task Assign_rejects_courier_without_profile()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "UNASSIGNED" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CourierProfile?)null);
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.CourierNotEligible, result.Failure);
    }

    [Fact]
    public async Task Register_courier_creates_profile_for_active_courier()
    {
        var courierId = Guid.NewGuid(); var zoneId = Guid.NewGuid();
        couriers.Setup(x => x.IsActiveCourierAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync((CourierProfile?)null);
        zones.Setup(x => x.ExistsAsync(zoneId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.AddCourierProfileAsync(It.IsAny<CourierProfile>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        couriers.Setup(x => x.GetNamesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [courierId] = "Courier" });
        repository.Setup(x => x.GetWorkloadsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await Create().RegisterCourierAsync(courierId, zoneId, "Van", "ABC-123", default);

        Assert.True(result.IsSuccess);
        Assert.Equal(zoneId, result.Profile!.ZoneId);
        Assert.True(result.Profile.IsAvailable);
        repository.Verify(x => x.AddCourierProfileAsync(It.Is<CourierProfile>(p => p.CourierId == courierId
            && p.VehicleType == "Van" && p.LicensePlate == "ABC-123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_rejects_non_courier()
    {
        couriers.Setup(x => x.IsActiveCourierAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var result = await Create().RegisterCourierAsync(Guid.NewGuid(), null, null, null, default);
        Assert.Equal(DispatchFailure.CourierNotEligible, result.Failure);
    }

    [Fact]
    public async Task Register_rejects_duplicate_profile()
    {
        var courierId = Guid.NewGuid();
        couriers.Setup(x => x.IsActiveCourierAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId });
        var result = await Create().RegisterCourierAsync(courierId, null, null, null, default);
        Assert.Equal(DispatchFailure.CourierAlreadyRegistered, result.Failure);
    }

    [Fact]
    public async Task Update_profile_changes_zone_and_preserves_omitted_fields()
    {
        var courierId = Guid.NewGuid(); var zoneId = Guid.NewGuid();
        var profile = new CourierProfile { CourierId = courierId, ZoneId = Guid.NewGuid(), IsAvailable = true,
            VehicleType = "Van", LicensePlate = "ABC-123" };
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(profile);
        zones.Setup(x => x.ExistsAsync(zoneId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        couriers.Setup(x => x.GetNamesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [courierId] = "Courier" });
        repository.Setup(x => x.GetWorkloadsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());

        var result = await Create().UpdateCourierAsync(courierId, zoneId, null, null, null, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(zoneId, profile.ZoneId);
        Assert.True(profile.IsAvailable);
        Assert.Equal("Van", profile.VehicleType);
        Assert.Equal("ABC-123", profile.LicensePlate);
    }

    [Fact]
    public async Task Assign_returns_conflict_when_version_changed()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "UNASSIGNED" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId, IsAvailable = true });
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

    [Theory]
    [InlineData("DELIVERED")]
    [InlineData("FAILED")]
    public async Task Parcel_status_terminal_event_completes_assignment_once(string status)
    {
        await using var db = await CreateDatabaseAsync();
        var parcelId = Guid.NewGuid();
        db.Assignments.Add(new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD123", State = "ASSIGNED" });
        await db.SaveChangesAsync();
        var consumer = new ParcelStatusChangedConsumer(new DispatchRepository(db));
        var message = new ParcelStatusChangedMessage(Guid.NewGuid(), parcelId, status);
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        Assert.Equal("COMPLETED", (await db.Assignments.SingleAsync()).State);
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task Parcel_status_non_terminal_event_leaves_assignment_assigned()
    {
        await using var db = await CreateDatabaseAsync();
        db.Assignments.Add(new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "SD123", State = "ASSIGNED" });
        await db.SaveChangesAsync();
        var message = new ParcelStatusChangedMessage(Guid.NewGuid(), db.Assignments.Local.Single().ParcelId, "IN_TRANSIT");
        await new ParcelStatusChangedConsumer(new DispatchRepository(db)).HandleAsync(message, default);
        Assert.Equal("ASSIGNED", (await db.Assignments.SingleAsync()).State);
    }

    [Fact]
    public async Task Parcel_status_for_unknown_parcel_throws_for_retry_and_is_not_marked_processed()
    {
        await using var db = await CreateDatabaseAsync();
        var message = new ParcelStatusChangedMessage(Guid.NewGuid(), Guid.NewGuid(), "DELIVERED");
        await Assert.ThrowsAsync<DispatchEventRetryException>(() =>
            new ParcelStatusChangedConsumer(new DispatchRepository(db)).HandleAsync(message, default));
        Assert.Empty(await db.ProcessedEvents.ToListAsync());
    }

    [Fact]
    public async Task Workload_counts_only_assigned_rows_for_the_courier()
    {
        await using var db = await CreateDatabaseAsync();
        var courierId = Guid.NewGuid(); var otherCourier = Guid.NewGuid();
        db.Assignments.AddRange(
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "A", CourierId = courierId, State = "ASSIGNED" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "B", CourierId = courierId, State = "COMPLETED" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "C", CourierId = otherCourier, State = "ASSIGNED" });
        await db.SaveChangesAsync();
        var workload = await new DispatchRepository(db).GetWorkloadsAsync([courierId, Guid.NewGuid()], default);
        Assert.Equal(1, workload[courierId]);
        Assert.Equal(0, workload.GetValueOrDefault(Guid.NewGuid()));
    }

    [Fact]
    public async Task Summary_counts_profiles_availability_and_active_assignments()
    {
        await using var db = await CreateDatabaseAsync();
        db.CourierProfiles.AddRange(new CourierProfile { CourierId = Guid.NewGuid(), IsAvailable = true },
            new CourierProfile { CourierId = Guid.NewGuid(), IsAvailable = false });
        db.Assignments.Add(new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "A", State = "ASSIGNED" });
        db.Assignments.Add(new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "B", State = "COMPLETED" });
        await db.SaveChangesAsync();
        var summary = await new DispatchRepository(db).GetCourierSummaryAsync(default);
        Assert.Equal((2, 1, 1, 1), summary);
    }

    [Fact]
    public async Task Dashboard_summary_counts_unassigned_and_distinct_active_couriers()
    {
        await using var db = await CreateDatabaseAsync();
        var courierId = Guid.NewGuid();
        db.CourierProfiles.AddRange(new CourierProfile { CourierId = courierId, IsAvailable = true },
            new CourierProfile { CourierId = Guid.NewGuid(), IsAvailable = false });
        db.Assignments.AddRange(
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "U1", State = "UNASSIGNED" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "A1", CourierId = courierId, State = "ASSIGNED" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "A2", CourierId = courierId, State = "ASSIGNED" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "C1", CourierId = courierId, State = "COMPLETED" });
        await db.SaveChangesAsync();

        var summary = await new DispatchRepository(db).GetDashboardSummaryAsync(default);

        Assert.Equal((1, 1, 1, 2, 2), summary);
    }

    [Fact]
    public async Task User_role_consumer_creates_courier_profile_once()
    {
        await using var db = await CreateDatabaseAsync();
        var userId = Guid.NewGuid(); var eventId = Guid.NewGuid();
        var consumer = new UserRoleChangedConsumer(new DispatchRepository(db));
        var message = new UserRoleChangedMessage(eventId, userId, "COURIER");
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        Assert.Equal(1, await db.CourierProfiles.CountAsync(x => x.CourierId == userId));
        Assert.False((await db.CourierProfiles.SingleAsync()).IsAvailable);
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task User_status_consumer_marks_profile_unavailable_once()
    {
        await using var db = await CreateDatabaseAsync();
        var userId = Guid.NewGuid(); var eventId = Guid.NewGuid();
        db.CourierProfiles.Add(new CourierProfile { CourierId = userId, IsAvailable = true });
        await db.SaveChangesAsync();
        var consumer = new UserStatusChangedConsumer(new DispatchRepository(db));
        var message = new UserStatusChangedMessage(eventId, userId, false);
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        Assert.False((await db.CourierProfiles.SingleAsync()).IsAvailable);
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task User_role_consumer_marks_profile_unavailable_when_role_changes()
    {
        await using var db = await CreateDatabaseAsync();
        var userId = Guid.NewGuid(); var eventId = Guid.NewGuid();
        db.CourierProfiles.Add(new CourierProfile { CourierId = userId, IsAvailable = true });
        await db.SaveChangesAsync();
        var consumer = new UserRoleChangedConsumer(new DispatchRepository(db));
        var message = new UserRoleChangedMessage(eventId, userId, "CUSTOMER");
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        Assert.False((await db.CourierProfiles.SingleAsync()).IsAvailable);
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public void Courier_request_validators_reject_invalid_inputs()
    {
        Assert.False(new RegisterCourierValidator().Validate(new RegisterCourierRequest(Guid.Empty, null, null, null)).IsValid);
        Assert.False(new UpdateCourierValidator().Validate(new UpdateCourierRequest(null, null, null, null)).IsValid);
        Assert.False(new ListCouriersValidator().Validate(new ListCouriersRequest(null, null, 0, 101)).IsValid);
    }

    private static async Task<DispatchDbContext> CreateDatabaseAsync()
    {
        var db = new DispatchDbContext(new DbContextOptionsBuilder<DispatchDbContext>()
            .UseSqlite("Data Source=:memory:").Options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        return db;
    }
}
