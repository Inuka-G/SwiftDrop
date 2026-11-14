using FluentAssertions;
using Moq;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.Merchants.Business.Validators;
using SwiftDrop.Merchants.Data.Entities;
using SwiftDrop.Merchants.Data.Repositories;
using SwiftDrop.SharedKernel.Exceptions;
using Xunit;

namespace SwiftDrop.Merchants.UnitTests;

public sealed class ApiKeyTests
{
    private readonly Mock<IMerchantRepository> _repository = new();
    private readonly Mock<IMerchantEventPublisher> _eventPublisher = new();
    private readonly RegisterMerchantValidator _registerValidator = new();
    private readonly CreateApiKeyValidator _apiKeyValidator = new();

    private MerchantService CreateService() =>
        new(_repository.Object, _registerValidator, _apiKeyValidator, _eventPublisher.Object);

    [Fact]
    public async Task CreateApiKeyAsync_WhenMerchantIsApproved_ShouldGenerateKey_AndStoreHash()
    {
        // 1. Arrange: An approved merchant
        var merchant = new Merchant("Nike Store", "PV99999");
        merchant.Approve(Guid.NewGuid()); // Status is now "Approved"
        var merchantId = merchant.Id;

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();
        var command = new CreateApiKeyCommand("Shopify Store Key");

        // 2. Act: Generate the key
        var result = await service.CreateApiKeyAsync(merchantId, command);

        // 3. Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Shopify Store Key");
        result.PlainTextKey.Should().StartWith("sd_live_"); // Raw key given once
        result.KeyPrefix.Should().Be(result.PlainTextKey[..16]); // Prefix matches

        // Crucial Security Check: Database NEVER received the plain text key!
        _repository.Verify(r => r.AddApiKeyAsync(It.Is<ApiKey>(k =>
            k.MerchantId == merchantId &&
            k.KeyPrefix == result.KeyPrefix &&
            k.KeyHash != result.PlainTextKey && // Must be hashed!
            k.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateApiKeyAsync_WhenMerchantIsPending_ShouldThrowForbiddenException()
    {
        // 1. Arrange: A merchant who is still Pending
        var merchant = new Merchant("Nike Store", "PV99999"); // Status is "Pending" by default
        var merchantId = merchant.Id;

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();
        var command = new CreateApiKeyCommand("Unauthorized Key");

        // 2. Act & Assert: Must throw ForbiddenException!
        var act = () => service.CreateApiKeyAsync(merchantId, command);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("Only approved merchants can create API keys.");

        // Verify a key was NEVER saved
        _repository.Verify(r => r.AddApiKeyAsync(It.IsAny<ApiKey>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WithValidActiveKey_ShouldReturnMerchantAccessResult()
    {
        // 1. Arrange: Approved merchant + saved active key hash
        var rawKey = "sd_live_testsecret123456";
        var keyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();

        var merchant = new Merchant("Nike Store", "PV99999");
        merchant.Approve(Guid.NewGuid());

        var apiKey = new ApiKey(merchant.Id, "Production Store", "sd_live_testsecre", keyHash);

        _repository.Setup(r => r.GetApiKeyByHashAsync(keyHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);

        _repository.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();

        // 2. Act: Validate the raw key
        var result = await service.ValidateApiKeyAsync(rawKey);

        // 3. Assert: Must return merchant identity!
        result.Should().NotBeNull();
        result!.MerchantId.Should().Be(merchant.Id);
        result.BusinessName.Should().Be("Nike Store");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateApiKeyAsync_WhenKeyIsRevoked_ShouldReturnNull()
    {
        // 1. Arrange: An API key that has been revoked (IsActive = false)
        var rawKey = "sd_live_revokedsecret123";
        var keyHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();

        var merchantId = Guid.NewGuid();
        var apiKey = new ApiKey(merchantId, "Leaked Key", "sd_live_revokedse", keyHash);
        apiKey.Revoke(); // Flip to inactive!

        _repository.Setup(r => r.GetApiKeyByHashAsync(keyHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiKey);

        var service = CreateService();

        // 2. Act: Try validating the revoked key
        var result = await service.ValidateApiKeyAsync(rawKey);

        // 3. Assert: Must be rejected (null)!
        result.Should().BeNull();
    }
}

