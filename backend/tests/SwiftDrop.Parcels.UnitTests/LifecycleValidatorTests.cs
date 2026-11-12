using FluentAssertions;
using SwiftDrop.Parcels.Business.Validation;
using SwiftDrop.Parcels.Contracts;
using SwiftDrop.Parcels.Contracts.Dtos;

namespace SwiftDrop.Parcels.UnitTests;

public class LifecycleValidatorTests
{
    [Theory]
    [InlineData(ParcelStatuses.PickedUp, true)]
    [InlineData(ParcelStatuses.InTransit, true)]
    [InlineData(ParcelStatuses.OutForDelivery, true)]
    [InlineData(ParcelStatuses.Delivered, false)]
    [InlineData(ParcelStatuses.Created, false)]
    [InlineData("pickedup", false)]
    [InlineData("", false)]
    public void Status_update_accepts_only_in_progress_statuses(string status, bool valid) =>
        new UpdateDeliveryStatusValidator().Validate(new UpdateDeliveryStatusRequest(status, null))
            .IsValid.Should().Be(valid);

    [Fact]
    public void Status_note_is_limited_to_500_characters() =>
        new UpdateDeliveryStatusValidator().Validate(new UpdateDeliveryStatusRequest(ParcelStatuses.PickedUp, new string('x', 501)))
            .IsValid.Should().BeFalse();

    [Theory]
    [InlineData(FailureReasons.RecipientUnavailable, null, true)]
    [InlineData(FailureReasons.Other, "Gate locked, guard refused entry", true)]
    [InlineData(FailureReasons.Other, null, false)]
    [InlineData(FailureReasons.Other, "  ", false)]
    [InlineData("NotARealReason", null, false)]
    public void Failure_needs_a_known_reason_and_a_note_for_other(string reason, string? note, bool valid) =>
        new ReportFailureValidator().Validate(new ReportFailureRequest(reason, note)).IsValid.Should().Be(valid);

    [Fact]
    public void Recipient_name_is_limited_to_150_characters() =>
        new ConfirmDeliveryValidator().Validate(new ConfirmDeliveryRequest(new string('x', 151), null))
            .IsValid.Should().BeFalse();
}
