using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SwiftDrop.Parcels.Business.Lifecycle;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Contracts.Events;
using SwiftDrop.Parcels.UnitTests.TestSupport;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Parcels.UnitTests;

public sealed class ParcelLifecycleServiceTests : IDisposable
{
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    private readonly ParcelsTestDb db = new();
    private readonly ParcelLifecycleService service;
    private readonly Guid courierId = Guid.NewGuid();

    public ParcelLifecycleServiceTests() =>
        service = new ParcelLifecycleService(db.Repository, db.Photos, db.Clock);

    public void Dispose() => db.Dispose();

    [Fact]
    public async Task Assigned_courier_moves_parcel_forward_with_history_and_event()
    {
        var parcel = await db.AddParcelAsync(courierId);

        var result = await service.UpdateStatusAsync(courierId, parcel.Id,
            new UpdateDeliveryStatusRequest(ParcelStatuses.PickedUp, "  Collected from front desk "), default);

        result.CurrentStatus.Should().Be(ParcelStatuses.PickedUp);
        result.AllowedNextStatuses.Should().Equal(ParcelStatuses.InTransit, ParcelStatuses.Failed);

        await using var check = db.NewContext();
        (await check.Parcels.SingleAsync()).CurrentStatus.Should().Be(ParcelStatuses.PickedUp);
        var history = await check.StatusHistory.SingleAsync();
        history.FromStatus.Should().Be(ParcelStatuses.Created);
        history.ToStatus.Should().Be(ParcelStatuses.PickedUp);
        history.Note.Should().Be("Collected from front desk");
        history.ChangedByRole.Should().Be(Roles.Courier);

        var outbox = await check.OutboxMessages.SingleAsync();
        outbox.RoutingKey.Should().Be(ParcelEventRoutingKeys.StatusChanged);
        var evt = JsonSerializer.Deserialize<ParcelStatusChangedIntegrationEvent>(outbox.Payload, JsonSerializerOptions.Web)!;
        evt.EventId.Should().Be(outbox.Id);
        evt.ParcelId.Should().Be(parcel.Id);
        evt.CourierId.Should().Be(courierId);
        evt.PreviousStatus.Should().Be(ParcelStatuses.Created);
        evt.NewStatus.Should().Be(ParcelStatuses.PickedUp);
    }

    [Fact]
    public async Task Status_event_carries_no_addresses_phones_or_names()
    {
        var parcel = await db.AddParcelAsync(courierId);

        await service.UpdateStatusAsync(courierId, parcel.Id,
            new UpdateDeliveryStatusRequest(ParcelStatuses.PickedUp, "Nimal Silva said hi"), default);

        await using var check = db.NewContext();
        var payload = (await check.OutboxMessages.SingleAsync()).Payload;
        payload.Should().NotContain("Galle Road").And.NotContain("Kandy")
            .And.NotContain("+9477").And.NotContain("Nimal").And.NotContain("Perera");
    }

    [Fact]
    public async Task Courier_cannot_update_another_couriers_delivery()
    {
        var parcel = await db.AddParcelAsync(courierId: Guid.NewGuid());

        var act = () => service.UpdateStatusAsync(courierId, parcel.Id,
            new UpdateDeliveryStatusRequest(ParcelStatuses.PickedUp, null), default);

        await act.Should().ThrowAsync<ForbiddenException>();
        await using var check = db.NewContext();
        (await check.Parcels.SingleAsync()).CurrentStatus.Should().Be(ParcelStatuses.Created);
        (await check.StatusHistory.CountAsync()).Should().Be(0);
        (await check.OutboxMessages.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Courier_cannot_read_another_couriers_delivery()
    {
        var parcel = await db.AddParcelAsync(courierId: Guid.NewGuid());

        var act = () => service.GetCourierDeliveryAsync(courierId, parcel.Id, default);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Unknown_parcel_is_not_found()
    {
        var act = () => service.GetCourierDeliveryAsync(courierId, Guid.NewGuid(), default);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Skipping_a_stage_is_a_conflict()
    {
        var parcel = await db.AddParcelAsync(courierId);

        var act = () => service.UpdateStatusAsync(courierId, parcel.Id,
            new UpdateDeliveryStatusRequest(ParcelStatuses.OutForDelivery, null), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Theory]
    [InlineData(ParcelStatuses.Delivered)]
    [InlineData(ParcelStatuses.Failed)]
    public async Task Status_update_cannot_close_a_delivery(string status)
    {
        var parcel = await db.AddParcelAsync(courierId);

        var act = () => service.UpdateStatusAsync(courierId, parcel.Id, new UpdateDeliveryStatusRequest(status, null), default);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Delivery_needs_recipient_name_or_photo()
    {
        var parcel = await MoveToOutForDeliveryAsync();

        var act = () => service.ConfirmDeliveryAsync(courierId, parcel, new ConfirmDeliveryRequest("  ", null), null, default);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task Delivery_with_photo_stores_photo_and_proof()
    {
        var parcel = await MoveToOutForDeliveryAsync();

        var result = await service.ConfirmDeliveryAsync(courierId, parcel,
            new ConfirmDeliveryRequest("Nimal Silva", "Left with recipient"),
            new ProofPhotoUpload(new MemoryStream(JpegBytes), JpegBytes.Length), default);

        result.CurrentStatus.Should().Be(ParcelStatuses.Delivered);
        result.AllowedNextStatuses.Should().BeEmpty();
        db.Photos.Files.Should().ContainSingle().Which.Key.Should().EndWith(".jpg");

        var actor = new ParcelActor(courierId, Roles.Courier);
        var proof = await service.GetProofAsync(actor, parcel, default);
        proof!.RecipientName.Should().Be("Nimal Silva");
        proof.HasPhoto.Should().BeTrue();
        var photo = await service.OpenProofPhotoAsync(actor, parcel, default);
        photo!.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task Delivery_rejects_a_file_that_is_not_an_image()
    {
        var parcel = await MoveToOutForDeliveryAsync();
        var fake = "<script>alert(1)</script>"u8.ToArray();

        var act = () => service.ConfirmDeliveryAsync(courierId, parcel, new ConfirmDeliveryRequest("Nimal", null),
            new ProofPhotoUpload(new MemoryStream(fake), fake.Length), default);

        await act.Should().ThrowAsync<BadRequestException>();
        db.Photos.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Delivery_before_out_for_delivery_is_a_conflict_and_stores_no_photo()
    {
        var parcel = await db.AddParcelAsync(courierId);

        var act = () => service.ConfirmDeliveryAsync(courierId, parcel.Id, new ConfirmDeliveryRequest("Nimal", null),
            new ProofPhotoUpload(new MemoryStream(JpegBytes), JpegBytes.Length), default);

        await act.Should().ThrowAsync<ConflictException>();
        db.Photos.Files.Should().BeEmpty();
    }

    [Fact]
    public async Task Failure_records_reason_and_event_carries_it()
    {
        var parcel = await MoveToOutForDeliveryAsync();
        db.Clock.Now = db.Clock.Now.AddMinutes(10);

        var result = await service.ReportFailureAsync(courierId, parcel,
            new ReportFailureRequest(FailureReasons.RecipientUnavailable, "No answer at door"), null, default);

        result.CurrentStatus.Should().Be(ParcelStatuses.Failed);
        var report = await service.GetFailureReportAsync(new ParcelActor(courierId, Roles.Courier), parcel, default);
        report!.Reason.Should().Be(FailureReasons.RecipientUnavailable);
        report.Note.Should().Be("No answer at door");

        await using var check = db.NewContext();
        var last = await check.OutboxMessages.OrderByDescending(m => m.CreatedAtUtc).FirstAsync();
        var evt = JsonSerializer.Deserialize<ParcelStatusChangedIntegrationEvent>(last.Payload, JsonSerializerOptions.Web)!;
        evt.NewStatus.Should().Be(ParcelStatuses.Failed);
        evt.FailureReason.Should().Be(FailureReasons.RecipientUnavailable);
    }

    [Fact]
    public async Task Failure_can_include_an_attempt_photo()
    {
        var parcel = await MoveToOutForDeliveryAsync();

        await service.ReportFailureAsync(courierId, parcel, new ReportFailureRequest(FailureReasons.AccessRestricted, "Gate locked"),
            new ProofPhotoUpload(new MemoryStream(JpegBytes), JpegBytes.Length), default);

        var actor = new ParcelActor(courierId, Roles.Courier);
        (await service.GetFailureReportAsync(actor, parcel, default))!.HasPhoto.Should().BeTrue();
        var photo = await service.OpenFailurePhotoAsync(actor, parcel, default);
        photo!.ContentType.Should().Be("image/jpeg");
        (await service.OpenProofPhotoAsync(actor, parcel, default)).Should().BeNull();   // no delivery proof
    }

    [Fact]
    public async Task Delivered_parcel_cannot_then_fail()
    {
        var parcel = await MoveToOutForDeliveryAsync();
        await service.ConfirmDeliveryAsync(courierId, parcel, new ConfirmDeliveryRequest("Nimal", null), null, default);

        var act = () => service.ReportFailureAsync(courierId, parcel,
            new ReportFailureRequest(FailureReasons.WrongAddress, null), null, default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Courier_list_has_open_deliveries_and_those_closed_since_the_cutoff()
    {
        var open = await db.AddParcelAsync(courierId);
        var closedToday = await MoveToOutForDeliveryAsync();
        await service.ReportFailureAsync(courierId, closedToday, new ReportFailureRequest(FailureReasons.WrongAddress, null), null, default);
        await db.AddParcelAsync(courierId: Guid.NewGuid());   // someone else's

        var cutoff = db.Clock.Now.UtcDateTime.Date;
        var today = await service.GetCourierDeliveriesAsync(courierId, cutoff, default);
        var tomorrow = await service.GetCourierDeliveriesAsync(courierId, cutoff.AddDays(1), default);

        today.Select(d => d.Id).Should().BeEquivalentTo([open.Id, closedToday]);
        tomorrow.Select(d => d.Id).Should().Equal(open.Id);
    }

    [Theory]
    [InlineData(Roles.Admin, false, true)]
    [InlineData(Roles.Dispatcher, false, true)]
    [InlineData(Roles.Customer, true, true)]     // the customer who booked it
    [InlineData(Roles.Customer, false, false)]   // any other customer
    [InlineData(Roles.Courier, false, false)]    // a courier it is not assigned to
    [InlineData(Roles.Merchant, false, false)]
    public async Task History_is_visible_only_to_allowed_users(string role, bool isOwner, bool allowed)
    {
        var customerId = Guid.NewGuid();
        var parcel = await db.AddParcelAsync(courierId, customerId);
        var actor = new ParcelActor(isOwner ? customerId : Guid.NewGuid(), role);

        var act = () => service.GetHistoryAsync(actor, parcel.Id, default);

        if (allowed) await act.Should().NotThrowAsync();
        else await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task History_lists_every_change_in_order()
    {
        var parcel = await MoveToOutForDeliveryAsync();

        var history = await service.GetHistoryAsync(new ParcelActor(Guid.NewGuid(), Roles.Admin), parcel, default);

        history.Select(h => h.ToStatus).Should().Equal(
            ParcelStatuses.PickedUp, ParcelStatuses.InTransit, ParcelStatuses.OutForDelivery);
    }

    private async Task<Guid> MoveToOutForDeliveryAsync()
    {
        var parcel = await db.AddParcelAsync(courierId);
        foreach (var status in new[] { ParcelStatuses.PickedUp, ParcelStatuses.InTransit, ParcelStatuses.OutForDelivery })
        {
            db.Clock.Now = db.Clock.Now.AddMinutes(10);
            await service.UpdateStatusAsync(courierId, parcel.Id, new UpdateDeliveryStatusRequest(status, null), default);
        }
        return parcel.Id;
    }
}
