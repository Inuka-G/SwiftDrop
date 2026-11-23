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
    private readonly Mock<IZoneLookup> zones = new();
    private DispatchService Create() => new(repository.Object, couriers.Object, parcels.Object, zones.Object);

    [Fact]
    public async Task Assign_delegates_authoritative_write_to_parcels_and_returns_its_timestamp()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        var parcel = new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD123", State = "UNASSIGNED" };
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId, IsAvailable = true });
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>())).ReturnsAsync(parcel);
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var assignedAt = DateTimeOffset.UtcNow;
        parcels.Setup(x => x.AssignAsync(parcelId, courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SwiftDrop.Parcels.Contracts.CourierAssignmentResult(Guid.NewGuid(), assignedAt));
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.True(result.IsSuccess);
        Assert.Equal(assignedAt, result.Value!.AssignedAt);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Courier_list_never_reports_an_inactive_identity_as_available()
    {
        var courierId = Guid.NewGuid();
        repository.Setup(x => x.ListCouriersAsync(null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<CourierProfile>) [new CourierProfile { CourierId = courierId, IsAvailable = true }], 1));
        repository.Setup(x => x.GetWorkloadsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, int>());
        couriers.Setup(x => x.GetNamesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [courierId] = "Courier" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await Create().ListCouriersAsync(null, null, 1, 20, default);

        Assert.False(result.Items.Single().IsAvailable);
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
    public async Task Assign_returns_conflict_when_parcels_rejects_assignment_race()
    {
        var parcelId = Guid.NewGuid(); var courierId = Guid.NewGuid();
        repository.Setup(x => x.GetAsync(parcelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParcelAssignment { State = "UNASSIGNED" });
        couriers.Setup(x => x.IsEligibleAsync(courierId, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repository.Setup(x => x.GetCourierProfileAsync(courierId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CourierProfile { CourierId = courierId, IsAvailable = true });
        parcels.Setup(x => x.AssignAsync(parcelId, courierId, It.IsAny<CancellationToken>())).ReturnsAsync((SwiftDrop.Parcels.Contracts.CourierAssignmentResult?)null);
        var result = await Create().AssignAsync(parcelId, courierId, Guid.NewGuid(), default);
        Assert.Equal(DispatchFailure.ParcelNotAssignable, result.Failure);
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

    [Fact]
    public async Task Parcel_list_returns_assigned_and_completed_rows_with_status_filters_and_pagination()
    {
        await using var db = await CreateDatabaseAsync();
        db.Assignments.AddRange(
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "SD-LIST-1", State = "COMPLETED", CurrentStatus = "Delivered" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "SD-LIST-2", State = "COMPLETED", CurrentStatus = "Delivered" },
            new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "SD-LIST-3", State = "ASSIGNED", CurrentStatus = "In Transit" });
        await db.SaveChangesAsync();

        var (items, total) = await new DispatchRepository(db).ListParcelsAsync("dElIvErEd", "sd-list", 1, 1, default);

        Assert.Equal(2, total);
        Assert.Single(items);
        Assert.Equal("Delivered", items[0].CurrentStatus);
    }

    [Fact]
    public async Task Courier_assigned_event_updates_projection_idempotently()
    {
        var options = new DbContextOptionsBuilder<DispatchDbContext>().UseSqlite("Data Source=:memory:").Options;
        await using var db = new DispatchDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();
        var repository = new DispatchRepository(db);
        var parcelId = Guid.NewGuid();
        var courierId = Guid.NewGuid();
        var eventId = Guid.NewGuid();
        var assignedAt = DateTimeOffset.UtcNow;
        await repository.ApplyCourierAssignedAsync(eventId, parcelId, "SD-000001-A1B2C3", courierId, assignedAt, default);
        await repository.ApplyCourierAssignedAsync(eventId, parcelId, "SD-000001-A1B2C3", courierId, assignedAt, default);

        var assignment = await repository.GetAsync(parcelId, default);
        Assert.Equal("ASSIGNED", assignment!.State);
        Assert.Equal(courierId, assignment.CourierId);
        Assert.Single(await db.ProcessedEvents.ToListAsync());
    }

    [Theory]
    [InlineData("dElIvErEd")]
    [InlineData("Failed")]
    public async Task Parcel_status_terminal_event_completes_assignment_once(string status)
    {
        await using var db = await CreateDatabaseAsync();
        var parcelId = Guid.NewGuid();
        db.Assignments.Add(new ParcelAssignment { ParcelId = parcelId, TrackingNumber = "SD123", State = "ASSIGNED" });
        await db.SaveChangesAsync();
        var consumer = new ParcelStatusChangedConsumer(new DispatchRepository(db));
        var message = new ParcelStatusChangedMessage(Guid.NewGuid(), parcelId, "SD123", null, status,
            DateTimeOffset.UtcNow.AddSeconds(1));
        await consumer.HandleAsync(message, default);
        await consumer.HandleAsync(message, default);
        Assert.Equal("COMPLETED", (await db.Assignments.SingleAsync()).State);
        Assert.Equal(status, (await db.Assignments.SingleAsync()).CurrentStatus);
        Assert.Equal(1, await db.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task Parcel_status_non_terminal_event_leaves_assignment_assigned()
    {
        await using var db = await CreateDatabaseAsync();
        db.Assignments.Add(new ParcelAssignment { ParcelId = Guid.NewGuid(), TrackingNumber = "SD123", State = "ASSIGNED" });
        await db.SaveChangesAsync();
        var message = new ParcelStatusChangedMessage(Guid.NewGuid(), db.Assignments.Local.Single().ParcelId,
            "SD123", null, "In Transit", DateTimeOffset.UtcNow.AddSeconds(1));
        await new ParcelStatusChangedConsumer(new DispatchRepository(db)).HandleAsync(message, default);
        Assert.Equal("ASSIGNED", (await db.Assignments.SingleAsync()).State);
        Assert.Equal("In Transit", (await db.Assignments.SingleAsync()).CurrentStatus);
    }

    [Fact]
    public async Task Status_before_creation_creates_placeholder_and_later_creation_enriches_it_without_regression()
    {
        await using var db = await CreateDatabaseAsync();
        var repository = new DispatchRepository(db);
        var parcelId = Guid.NewGuid();
        var deliveredAt = DateTimeOffset.UtcNow;
        var statusEvent = new ParcelStatusChangedMessage(Guid.NewGuid(), parcelId, "SD123", null,
            "Delivered", deliveredAt);
        await new ParcelStatusChangedConsumer(repository).HandleAsync(statusEvent, default);

        var creationEvent = new ParcelCreatedMessage(Guid.NewGuid(), parcelId, "SD123", Guid.NewGuid(), deliveredAt.AddMinutes(-2));
        await new ParcelCreatedConsumer(repository).HandleAsync(creationEvent, default);

        var assignment = await db.Assignments.SingleAsync();
        Assert.Equal("COMPLETED", assignment.State);
        Assert.Equal("Delivered", assignment.CurrentStatus);
        Assert.Equal(creationEvent.ZoneId, assignment.ZoneId);
        Assert.Equal(2, await db.ProcessedEvents.CountAsync());
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
