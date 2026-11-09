using FluentAssertions;
using SwiftDrop.Parcels.Business.Validation;
using SwiftDrop.Parcels.Contracts.Dtos;
using Xunit;

namespace SwiftDrop.Parcels.UnitTests;

public sealed class CreateParcelValidatorTests
{
    private readonly CreateParcelValidator _validator = new();

    private static CreateParcelRequest CreateValidRequest() => new(
        ZoneId: Guid.NewGuid(),
        PickupAddress: "123 Galle Road, Colombo 03",
        PickupContactName: "Kasun Perera",
        PickupContactPhone: "+94771234567",
        DropoffAddress: "456 Kandy Road, Kadawatha",
        RecipientName: "Nimal Silva",
        RecipientPhone: "+94719876543",
        WeightKg: 2.5m,
        SizeCategory: "Medium",
        PaymentMethod: "COD"
    );

    [Fact]
    public async Task Validate_ShouldPass_WhenRequestIsValid()
    {
        var request = CreateValidRequest();

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1.5)]
    public async Task Validate_ShouldFail_WhenWeightIsZeroOrNegative(decimal invalidWeight)
    {
        var request = CreateValidRequest() with { WeightKg = invalidWeight };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParcelRequest.WeightKg));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenWeightExceedsFiftyKg()
    {
        var request = CreateValidRequest() with { WeightKg = 50.1m };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParcelRequest.WeightKg));
    }

    [Theory]
    [InlineData("invalid-phone")]
    [InlineData("123")]
    [InlineData("abc-def-ghij")]
    public async Task Validate_ShouldFail_WhenRecipientPhoneIsInvalid(string invalidPhone)
    {
        var request = CreateValidRequest() with { RecipientPhone = invalidPhone };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParcelRequest.RecipientPhone));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_ShouldFail_WhenAddressesAreEmpty(string emptyAddress)
    {
        var request = CreateValidRequest() with { PickupAddress = emptyAddress, DropoffAddress = emptyAddress };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParcelRequest.PickupAddress));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParcelRequest.DropoffAddress));
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenZoneIdIsEmpty()
    {
        var request = CreateValidRequest() with { ZoneId = Guid.Empty };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateParcelRequest.ZoneId));
    }
}