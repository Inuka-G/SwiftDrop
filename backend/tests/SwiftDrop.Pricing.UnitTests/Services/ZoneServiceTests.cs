
using FluentAssertions;
using Moq;
using SwiftDrop.Pricing.Business.Services;
using SwiftDrop.Pricing.Data.Entities;
using SwiftDrop.Pricing.Data.Repositories;
using SwiftDrop.Pricing.Contracts;
using SwiftDrop.SharedKernel.Exceptions;

namespace SwiftDrop.Pricing.UnitTests.Services;

public class ZoneServiceTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnAllZones()
    {
        // Arrange
        var zones = new List<Zone>
        {
            new Zone("Colombo", "Colombo district"),
            new Zone("Kandy", "Kandy district"),
            new Zone("Galle", "Galle district")
        };

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(zones);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Select(z => z.ZoneName)
            .Should()
            .ContainInOrder("Colombo", "Kandy", "Galle");
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoZonesExist()
    {
        // Arrange
        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Zone>());

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.GetAllAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnZone_WhenZoneExists()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var zone = new Zone("Colombo", "Colombo district");

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.GetAsync(zoneId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ZoneName.Should().Be("Colombo");
        result.Description.Should().Be("Colombo district");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_ShouldReturnNull_WhenZoneDoesNotExist()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Zone?)null);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.GetAsync(zoneId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnZone()
    {
        // Arrange
        var command = new CreateZoneCommand("Colombo", "Colombo district");
        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.AddAsync(It.IsAny<Zone>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        repoMock
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.CreateAsync(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ZoneName.Should().Be("Colombo");
        result.Description.Should().Be("Colombo district");
        result.IsActive.Should().BeTrue();

        repoMock.Verify(repo => repo.AddAsync(
            It.Is<Zone>(z =>
                z.ZoneName == "Colombo" &&
                z.Description == "Colombo district"),
            It.IsAny<CancellationToken>()), Times.Once);

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateZone_WhenZoneExists()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var zone = new Zone("Colombo", "Old description");

        var command = new UpdateZoneCommand(
            "Western Province",
            "Updated description");

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdForUpdateAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        repoMock
            .Setup(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdateAsync(zoneId, command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.ZoneName.Should().Be("Western Province");
        result.Description.Should().Be("Updated description");

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenZoneDoesNotExist()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdForUpdateAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Zone?)null);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdateAsync(
            zoneId,
            new UpdateZoneCommand("Western Province", "Updated description"),
            CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldActivateZone()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var zone = new Zone("Colombo", "Colombo district");
        zone.SetActive(false);

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdForUpdateAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdateStatusAsync(zoneId, true, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeTrue();

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldDeactivateZone()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var zone = new Zone("Colombo", "Colombo district");

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdForUpdateAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(zone);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdateStatusAsync(zoneId, false, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.IsActive.Should().BeFalse();

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldReturnNull_WhenZoneDoesNotExist()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetByIdForUpdateAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Zone?)null);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdateStatusAsync(zoneId, true, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdatePricingRuleAsync_ShouldUpdateRates_WhenRuleExists()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var rule = new PricingRule(zoneId, 500m, 100m, ServiceType.Standard);
        var command = new UpdatePricingRuleCommand(600m, 150m);

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetPricingRuleForUpdateAsync(
                zoneId, ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rule);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdatePricingRuleAsync(
            zoneId, ruleId, command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.BaseRate.Should().Be(600m);
        result.AdditionalKgRate.Should().Be(150m);
        result.ZoneId.Should().Be(zoneId);

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePricingRuleAsync_ShouldThrowException_WhenBaseRateIsNegative()
    {
        // Arrange
        var repoMock = new Mock<IZoneRepository>();
        var service = new ZoneService(repoMock.Object);
        var command = new UpdatePricingRuleCommand(-100m, 150m);

        // Act
        var act = async () => await service.UpdatePricingRuleAsync(
            Guid.NewGuid(), Guid.NewGuid(), command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Pricing rates cannot be negative.");
    }

    [Fact]
    public async Task UpdatePricingRuleAsync_ShouldThrowException_WhenAdditionalKgRateIsNegative()
    {
        // Arrange
        var repoMock = new Mock<IZoneRepository>();
        var service = new ZoneService(repoMock.Object);
        var command = new UpdatePricingRuleCommand(600m, -50m);

        // Act
        var act = async () => await service.UpdatePricingRuleAsync(
            Guid.NewGuid(), Guid.NewGuid(), command, CancellationToken.None);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Pricing rates cannot be negative.");
    }

    [Fact]
    public async Task UpdatePricingRuleAsync_ShouldReturnNull_WhenRuleDoesNotExist()
    {
        // Arrange
        var zoneId = Guid.NewGuid();
        var ruleId = Guid.NewGuid();

        var repoMock = new Mock<IZoneRepository>();

        repoMock
            .Setup(repo => repo.GetPricingRuleForUpdateAsync(
                zoneId, ruleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PricingRule?)null);

        var service = new ZoneService(repoMock.Object);

        // Act
        var result = await service.UpdatePricingRuleAsync(
            zoneId,
            ruleId,
            new UpdatePricingRuleCommand(600m, 150m),
            CancellationToken.None);

        // Assert
        result.Should().BeNull();

        repoMock.Verify(repo => repo.SaveChangesAsync(
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(ServiceType.Standard)]
    [InlineData(ServiceType.Express)]
    public async Task CreatePricingRuleAsync_ShouldCreateSelectedServiceType(ServiceType serviceType)
    {
        var zoneId = Guid.NewGuid();
        PricingRule? addedRule = null;
        var repository = new Mock<IZoneRepository>();
        repository.Setup(repo => repo.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Zone("Test zone", null));
        repository.Setup(repo => repo.PricingRuleExistsAsync(zoneId, serviceType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(repo => repo.AddPricingRuleAsync(It.IsAny<PricingRule>(), It.IsAny<CancellationToken>()))
            .Callback<PricingRule, CancellationToken>((rule, _) => addedRule = rule)
            .Returns(Task.CompletedTask);

        var service = new ZoneService(repository.Object);
        var result = await service.CreatePricingRuleAsync(
            zoneId,
            new CreatePricingRuleCommand(serviceType, 350m, 80m, false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.ZoneId.Should().Be(zoneId);
        result.ServiceType.Should().Be(serviceType);
        result.BaseRate.Should().Be(350m);
        result.AdditionalKgRate.Should().Be(80m);
        result.IsActive.Should().BeFalse();
        addedRule.Should().NotBeNull();
        repository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreatePricingRuleAsync_ShouldRejectDuplicateServiceType()
    {
        var zoneId = Guid.NewGuid();
        var repository = new Mock<IZoneRepository>();
        repository.Setup(repo => repo.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Zone("Test zone", null));
        repository.Setup(repo => repo.PricingRuleExistsAsync(zoneId, ServiceType.Standard, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var service = new ZoneService(repository.Object);
        var act = () => service.CreatePricingRuleAsync(zoneId,
            new CreatePricingRuleCommand(ServiceType.Standard, 350m, 80m, true), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("A Standard pricing rule already exists for this zone.");
        repository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePricingRuleAsync_ShouldRejectUnsupportedServiceType()
    {
        var repository = new Mock<IZoneRepository>();
        var service = new ZoneService(repository.Object);
        var act = () => service.CreatePricingRuleAsync(Guid.NewGuid(),
            new CreatePricingRuleCommand((ServiceType)3, 350m, 80m, true), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("Invalid service type. Use Standard (1) or Express (2).");
        repository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(-1, 80)]
    [InlineData(350, -1)]
    [InlineData(350.001, 80)]
    [InlineData(100000000, 80)]
    public async Task CreatePricingRuleAsync_ShouldRejectInvalidPricingValues(decimal baseRate, decimal additionalKgRate)
    {
        var repository = new Mock<IZoneRepository>();
        var service = new ZoneService(repository.Object);
        var act = () => service.CreatePricingRuleAsync(Guid.NewGuid(),
            new CreatePricingRuleCommand(ServiceType.Standard, baseRate, additionalKgRate, true), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>();
        repository.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePricingRuleAsync_ShouldReturnNull_WhenZoneDoesNotExist()
    {
        var zoneId = Guid.NewGuid();
        var repository = new Mock<IZoneRepository>();
        repository.Setup(repo => repo.GetByIdAsync(zoneId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Zone?)null);

        var service = new ZoneService(repository.Object);
        var result = await service.CreatePricingRuleAsync(zoneId,
            new CreatePricingRuleCommand(ServiceType.Standard, 350m, 80m, true), CancellationToken.None);

        result.Should().BeNull();
        repository.Verify(repo => repo.AddPricingRuleAsync(It.IsAny<PricingRule>(), It.IsAny<CancellationToken>()), Times.Never);
        repository.Verify(repo => repo.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
