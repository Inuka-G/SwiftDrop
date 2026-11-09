
using FluentAssertions;
using Moq;
using SwiftDrop.Parcels.Business.Services;
using SwiftDrop.Parcels.Contracts.Dtos;
using SwiftDrop.Parcels.Data.Entities;
using SwiftDrop.Parcels.Data.Repositories;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.SharedKernel.Exceptions;
using Xunit;

namespace SwiftDrop.Parcels.UnitTests;

public sealed class ParcelServiceTests
{
    private readonly Mock<IParcelRepository> _repository = new();
    private readonly Mock<IPricingService> _pricingService = new();

    private ParcelService CreateService() =>
        new(_repository.Object, _pricingService.Object);

    private static CreateParcelRequest CreateSampleRequest(Guid zoneId) => new(
        ZoneId: zoneId,
        PickupAddress: "123 Galle Road, Colombo 03",
        PickupContactName: "Kasun Perera",
        PickupContactPhone: "+94771234567",
        DropoffAddress: "456 Main Street, Kandy",
        RecipientName: "Nimal Silva",
        RecipientPhone: "+94719876543",
        WeightKg: 3.0m,
        SizeCategory: "Medium",
        PaymentMethod: "COD"
    );

    [Fact]
    public async Task CreateParcelAsync_ShouldCalculateFee_AndSaveParcel_WithGeneratedTrackingNumber()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var request = CreateSampleRequest(zoneId);

        _pricingService
        .Setup(p => p.EstimateAsync(
            zoneId,
            request.WeightKg,
            ServiceType.Standard,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeeEstimateDto(
                zoneId,
                "Colombo to Kandy",
                350m,
                150m,
                500m));

        Parcel? savedParcel = null;

        _repository
            .Setup(r => r.AddAsync(
                It.IsAny<Parcel>(),
                It.IsAny<CancellationToken>()))
            .Callback<Parcel, CancellationToken>((p, _) => savedParcel = p)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var result = await service.CreateParcelAsync(
            customerId,
            request,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.EstimatedFee.Should().Be(500m);
        result.Status.Should().Be("Created");
        result.TrackingNumber.Should()
            .MatchRegex(@"^SD-\d{6}-[A-F0-9]{6}$");

        savedParcel.Should().NotBeNull();
        savedParcel!.CustomerId.Should().Be(customerId);
        savedParcel.TrackingNumber.Should().Be(result.TrackingNumber);
        savedParcel.EstimatedFee.Should().Be(500m);

        _repository.Verify(
            r => r.AddAsync(
                It.IsAny<Parcel>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _repository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateParcelAsync_ShouldThrowBadRequestException_WhenZonePricingFails()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var zoneId = Guid.NewGuid();
        var request = CreateSampleRequest(zoneId);

        _pricingService
         .Setup(p => p.EstimateAsync(
            zoneId,
            request.WeightKg,
            ServiceType.Standard,
            It.IsAny<CancellationToken>()))
         .ReturnsAsync((FeeEstimateDto?)null);

        var service = CreateService();

        // Act & Assert
        var act = () => service.CreateParcelAsync(
            customerId,
            request,
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<BadRequestException>()
            .WithMessage("*pricing could not be calculated*");

        _repository.Verify(
            r => r.AddAsync(
                It.IsAny<Parcel>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        _repository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPublicTrackingAsync_ShouldMaskDestinationArea_ForPrivacy()
    {
        // Arrange
        var trackingNumber = "SD-261109-A1B2C3";

        var parcel = new Parcel(
            customerId: Guid.NewGuid(),
            trackingNumber: trackingNumber,
            zoneId: Guid.NewGuid(),
            pickupAddress: "123 Galle Road, Colombo 03",
            pickupContactName: "Kasun",
            pickupContactPhone: "+94771234567",
            dropoffAddress: "No. 45, Peradeniya Road, Kandy",
            recipientName: "Nimal",
            recipientPhone: "+94719876543",
            weightKg: 2m,
            sizeCategory: "Small",
            estimatedFee: 450m,
            paymentMethod: "COD"
        );

        _repository
            .Setup(r => r.GetByTrackingNumberAsync(
                trackingNumber,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(parcel);

        var service = CreateService();

        // Act
        var result = await service.GetPublicTrackingAsync(
            trackingNumber,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.TrackingNumber.Should().Be(trackingNumber);
        result.CurrentStatus.Should().Be("Created");
        result.DestinationArea.Should().Be("Kandy");
    }

    [Fact]
    public async Task GetPublicTrackingAsync_ShouldReturnNull_WhenParcelDoesNotExist()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByTrackingNumberAsync(
                "NON-EXISTENT",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Parcel?)null);

        var service = CreateService();

        // Act
        var result = await service.GetPublicTrackingAsync(
            "NON-EXISTENT",
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
public async Task GetCustomerHistoryAsync_ShouldReturnOnlyCustomersParcels()
{
    var customerId = Guid.NewGuid();

    var parcels = new List<Parcel>
    {
        new(
            customerId,
            "SD-261109-A1B2C3",
            Guid.NewGuid(),
            "123 Galle Road, Colombo",
            "Kasun",
            "+94771234567",
            "456 Main Street, Kandy",
            "Nimal",
            "+94719876543",
            3m,
            "Medium",
            500m,
            "COD"
        )
    };

    _repository
        .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(parcels);

    var service = CreateService();

    var result = await service.GetCustomerHistoryAsync(customerId, CancellationToken.None);

    result.Should().HaveCount(1);
    result[0].TrackingNumber.Should().Be("SD-261109-A1B2C3");
    result[0].RecipientName.Should().Be("Nimal");

    _repository.Verify(
        r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()),
        Times.Once);
}

[Fact]
public async Task GetReceiptDetailsAsync_ShouldReturnNull_WhenParcelBelongsToAnotherCustomer()
{
    var ownerId = Guid.NewGuid();
    var anotherCustomerId = Guid.NewGuid();
    var parcelId = Guid.NewGuid();

    var parcel = new Parcel(
        ownerId,
        "SD-261109-A1B2C3",
        Guid.NewGuid(),
        "123 Galle Road, Colombo",
        "Kasun",
        "+94771234567",
        "456 Main Street, Kandy",
        "Nimal",
        "+94719876543",
        3m,
        "Medium",
        500m,
        "COD"
    );

    _repository
        .Setup(r => r.GetByIdAsync(parcelId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(parcel);

    var service = CreateService();

    var result = await service.GetReceiptDetailsAsync(
        parcelId,
        anotherCustomerId,
        CancellationToken.None);

    result.Should().BeNull();
}
[Fact]
public async Task GetReceiptDetailsAsync_ShouldReturnDetails_WhenParcelBelongsToCustomer()
{
    var customerId = Guid.NewGuid();

    var parcel = new Parcel(
        customerId,
        "SD-261109-A1B2C3",
        Guid.NewGuid(),
        "123 Galle Road, Colombo",
        "Kasun",
        "+94771234567",
        "456 Main Street, Kandy",
        "Nimal",
        "+94719876543",
        3m,
        "Medium",
        500m,
        "COD"
    );

    _repository
        .Setup(r => r.GetByIdAsync(parcel.Id, It.IsAny<CancellationToken>()))
        .ReturnsAsync(parcel);

    var service = CreateService();

    var result = await service.GetReceiptDetailsAsync(
        parcel.Id,
        customerId,
        CancellationToken.None);

    result.Should().NotBeNull();
    result!.TrackingNumber.Should().Be("SD-261109-A1B2C3");
    result.RecipientName.Should().Be("Nimal");
    result.EstimatedFee.Should().Be(500m);
}
[Fact]
public async Task GetReceiptDetailsAsync_ShouldReturnNull_WhenParcelDoesNotExist()
{
    var parcelId = Guid.NewGuid();
    var customerId = Guid.NewGuid();

    _repository
        .Setup(r => r.GetByIdAsync(parcelId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Parcel?)null);

    var service = CreateService();

    var result = await service.GetReceiptDetailsAsync(
        parcelId,
        customerId,
        CancellationToken.None);

    result.Should().BeNull();
}
}