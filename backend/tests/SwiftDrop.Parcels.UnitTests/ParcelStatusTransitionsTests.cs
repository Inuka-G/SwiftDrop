using FluentAssertions;
using SwiftDrop.Parcels.Business.Lifecycle;
using static SwiftDrop.Parcels.Contracts.ParcelStatuses;

namespace SwiftDrop.Parcels.UnitTests;

public class ParcelStatusTransitionsTests
{
    [Theory]
    [InlineData(Created, PickedUp)]
    [InlineData(PickedUp, InTransit)]
    [InlineData(InTransit, OutForDelivery)]
    [InlineData(OutForDelivery, Delivered)]
    [InlineData(Created, Failed)]
    [InlineData(PickedUp, Failed)]
    [InlineData(InTransit, Failed)]
    [InlineData(OutForDelivery, Failed)]
    public void Allows_each_forward_step_and_failure(string from, string to) =>
        ParcelStatusTransitions.CanMove(from, to).Should().BeTrue();

    [Theory]
    [InlineData(Created, InTransit)]          // skipping a stage
    [InlineData(Created, Delivered)]
    [InlineData(PickedUp, OutForDelivery)]
    [InlineData(InTransit, PickedUp)]         // going backwards
    [InlineData(OutForDelivery, Created)]
    [InlineData(PickedUp, PickedUp)]          // no-op
    [InlineData(Delivered, Failed)]           // terminal states are final
    [InlineData(Failed, OutForDelivery)]
    [InlineData("Unknown", PickedUp)]
    public void Rejects_invalid_moves(string from, string to) =>
        ParcelStatusTransitions.CanMove(from, to).Should().BeFalse();

    [Theory]
    [InlineData(Delivered)]
    [InlineData(Failed)]
    public void Terminal_statuses_have_no_next_status(string status) =>
        ParcelStatusTransitions.NextFrom(status).Should().BeEmpty();
}
