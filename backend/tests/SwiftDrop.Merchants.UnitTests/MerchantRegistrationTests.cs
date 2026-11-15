using FluentAssertions;
using Moq;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.Merchants.Business.Validators;
using SwiftDrop.Merchants.Contracts;
using SwiftDrop.Merchants.Data.Entities;
using SwiftDrop.Merchants.Data.Repositories;
using SwiftDrop.SharedKernel.Exceptions;
using Xunit;

namespace SwiftDrop.Merchants.UnitTests;

public sealed class MerchantRegistrationTests
{
    private readonly Mock<IMerchantRepository> _repository = new();
    private readonly Mock<IMerchantEventPublisher> _eventPublisher = new();
    private readonly Mock<IOAuthTokenService> _oauthTokenService = new();
    private readonly RegisterMerchantValidator _validator = new();
    private readonly CreateApiKeyValidator _apiKeyValidator = new();
    private readonly CreateClientCredentialValidator _clientValidator = new();

    private MerchantService CreateService() =>
        new(_repository.Object, _validator, _apiKeyValidator, _clientValidator, _oauthTokenService.Object, _eventPublisher.Object);

    [Fact]
    public async Task RegisterAsync_WithValidDetails_ShouldCreateMerchant_WithPendingStatus()
    {
        // Arrange
        var command = new RegisterMerchantCommand("Apex Retail", "PV12345", "https://apex.com/webhook");
        _repository.Setup(r => r.GetByRegNumberAsync("PV12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Merchant?)null);

        var service = CreateService();

        // Act
        var result = await service.RegisterAsync(command, userId: null);

        // Assert
        result.Should().NotBeNull();
        result.BusinessName.Should().Be("Apex Retail");
        result.BusinessRegNumber.Should().Be("PV12345");
        result.Status.Should().Be(MerchantStatus.Pending);

        _repository.Verify(r => r.AddAsync(It.Is<Merchant>(m =>
            m.BusinessName == "Apex Retail" &&
            m.BusinessRegNumber == "PV12345" &&
            m.Status == MerchantStatus.Pending), It.IsAny<CancellationToken>()), Times.Once);

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateRegNumber_ShouldThrowConflictException()
    {
        // Arrange
        var command = new RegisterMerchantCommand("Another Store", "PV12345", null);
        var existingMerchant = new Merchant("Existing Store", "PV12345");

        _repository.Setup(r => r.GetByRegNumberAsync("PV12345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingMerchant);

        var service = CreateService();

        // Act & Assert
        var act = () => service.RegisterAsync(command, userId: null);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("A merchant with this business registration number already exists.");

        _repository.Verify(r => r.AddAsync(It.IsAny<Merchant>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveAsync_WhenMerchantIsPending_ShouldSetStatusToApproved_AndRecordAdmin()
    {
        // 1. Arrange: A merchant in Pending status
        var merchantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var merchant = new Merchant("Apex Retail", "PV12345");

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();

        // 2. Act: Admin approves the merchant
        var result = await service.ApproveAsync(merchantId, adminId);

        // 3. Assert
        result.Status.Should().Be(MerchantStatus.Approved);
        result.ApprovedBy.Should().Be(adminId);
        result.ApprovedAt.Should().NotBeNull();

        _eventPublisher.Verify(p => p.PublishAsync(
            It.Is<MerchantApprovedEvent>(e => e.MerchantId == merchant.Id && e.BusinessName == "Apex Retail"),
            It.IsAny<CancellationToken>()), Times.Once);

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectAsync_WhenMerchantIsPending_ShouldSetStatusToRejected_AndRecordAdmin()
    {
        // 1. Arrange: A merchant in Pending status
        var merchantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var merchant = new Merchant("Apex Retail", "PV12345");

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();

        // 2. Act: Admin rejects the merchant
        var result = await service.RejectAsync(merchantId, adminId);

        // 3. Assert
        result.Status.Should().Be(MerchantStatus.Rejected);
        result.ApprovedBy.Should().Be(adminId);
        result.ApprovedAt.Should().NotBeNull();

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveAsync_WhenMerchantNotFound_ShouldThrowNotFoundException()
    {
        // 1. Arrange: Repository returns null (merchant does not exist)
        var merchantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Merchant?)null);

        var service = CreateService();

        // 2. Act & Assert: Calling ApproveAsync must throw NotFoundException
        var act = () => service.ApproveAsync(merchantId, adminId);
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("Merchant not found.");

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}

