
using FluentAssertions;
using Moq;
using SwiftDrop.Pricing.Business.Services;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Data.Repositories;

namespace SwiftDrop.Pricing.UnitTests.Services;

public class PricingServiceTests
{
    [Fact]
    public async Task EstimateAsync_ShouldCalculateStandardDeliveryFee()
    {
        // Arrange
        var zoneId = Guid.NewGuid();

        var zone = new Zone("Colombo", "Colombo district");

        var pricingRule = new PricingRule(
            zoneId,
            500m,
            100m,
            ServiceType.Standard
        );

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdAsync(
                zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        repoMock
            .Setup(repo => repo.GetPricingRuleAsync(
                zoneId, ServiceType.Standard,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricingRule);

        var service = new PricingService(repoMock.Object);

        // Act
        var result = await service.EstimateAsync(
            zoneId, 3m, ServiceType.Standard,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.BaseFee.Should().Be(500m);
        result.WeightFee.Should().Be(200m);
        result.Total.Should().Be(700m);
        result.ZoneName.Should().Be("Colombo");
    }


[Fact]
public async Task EstimateAsync_ShouldChargeOnlyBaseFee_WhenWeightIsBelowOneKg()
{
    // Arrange
    var zoneId = Guid.NewGuid();
    var zone = new Zone("Colombo", "Colombo district");

    var pricingRule = new PricingRule(
        zoneId,
        500m,
        100m,
        ServiceType.Standard
    );

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(zone);

    repoMock
        .Setup(repo => repo.GetPricingRuleAsync(
            zoneId, ServiceType.Standard,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(pricingRule);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 0.5m, ServiceType.Standard,
        CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.BaseFee.Should().Be(500m);
    result.WeightFee.Should().Be(0m);
    result.Total.Should().Be(500m);
}


[Fact]
public async Task EstimateAsync_ShouldChargeOnlyBaseFee_WhenWeightIsExactlyOneKg()
{
    // Arrange
    var zoneId = Guid.NewGuid();
    var zone = new Zone("Colombo", "Colombo district");

    var pricingRule = new PricingRule(
        zoneId,
        500m,
        100m,
        ServiceType.Standard
    );

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(zone);

    repoMock
        .Setup(repo => repo.GetPricingRuleAsync(
            zoneId, ServiceType.Standard,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(pricingRule);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 1m, ServiceType.Standard,
        CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.BaseFee.Should().Be(500m);
    result.WeightFee.Should().Be(0m);
    result.Total.Should().Be(500m);
}


[Fact]
public async Task EstimateAsync_ShouldReturnNull_WhenZoneDoesNotExist()
{
    // Arrange
    var zoneId = Guid.NewGuid();

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync((Zone?)null);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 2m, ServiceType.Standard,
        CancellationToken.None);

    // Assert
    result.Should().BeNull();

    repoMock.Verify(repo => repo.GetPricingRuleAsync(
        It.IsAny<Guid>(),
        It.IsAny<ServiceType>(),
        It.IsAny<CancellationToken>()), Times.Never);
}


[Fact]
public async Task EstimateAsync_ShouldReturnNull_WhenZoneIsInactive()
{
    // Arrange
    var zoneId = Guid.NewGuid();
    var zone = new Zone("Colombo", "Colombo district");

    zone.SetActive(false);

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(zone);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 2m, ServiceType.Standard,
        CancellationToken.None);

    // Assert
    result.Should().BeNull();

    repoMock.Verify(repo => repo.GetPricingRuleAsync(
        It.IsAny<Guid>(),
        It.IsAny<ServiceType>(),
        It.IsAny<CancellationToken>()), Times.Never);
}


[Fact]
public async Task EstimateAsync_ShouldReturnNull_WhenPricingRuleDoesNotExist()
{
    // Arrange
    var zoneId = Guid.NewGuid();
    var zone = new Zone("Colombo", "Colombo district");

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(zone);

    repoMock
        .Setup(repo => repo.GetPricingRuleAsync(
            zoneId, ServiceType.Standard,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((PricingRule?)null);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 2m, ServiceType.Standard,
        CancellationToken.None);

    // Assert
    result.Should().BeNull();
}


[Fact]
public async Task EstimateAsync_ShouldCalculateExpressDeliveryFee()
{
    // Arrange
    var zoneId = Guid.NewGuid();
    var zone = new Zone("Colombo", "Colombo district");

    var pricingRule = new PricingRule(
        zoneId,
        800m,
        200m,
        ServiceType.Express
    );

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(zone);

    repoMock
        .Setup(repo => repo.GetPricingRuleAsync(
            zoneId, ServiceType.Express,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(pricingRule);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 3m, ServiceType.Express,
        CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.BaseFee.Should().Be(800m);
    result.WeightFee.Should().Be(400m);
    result.Total.Should().Be(1200m);
}


[Fact]
public async Task EstimateAsync_ShouldChargeOnlyBaseFee_WhenWeightIsZero()
{
    // Arrange
    var zoneId = Guid.NewGuid();
    var zone = new Zone("Colombo", "Colombo district");

    var pricingRule = new PricingRule(
        zoneId,
        500m,
        100m,
        ServiceType.Standard
    );

    var repoMock = new Mock<IZoneRepository>();

    repoMock
        .Setup(repo => repo.GetByIdAsync(
            zoneId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(zone);

    repoMock
        .Setup(repo => repo.GetPricingRuleAsync(
            zoneId, ServiceType.Standard,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(pricingRule);

    var service = new PricingService(repoMock.Object);

    // Act
    var result = await service.EstimateAsync(
        zoneId, 0m, ServiceType.Standard,
        CancellationToken.None);

    // Assert
    result.Should().NotBeNull();
    result!.BaseFee.Should().Be(500m);
    result.WeightFee.Should().Be(0m);
    result.Total.Should().Be(500m);
}


}
