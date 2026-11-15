using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.Merchants.Business.Services;
using SwiftDrop.Merchants.Business.Validators;
using SwiftDrop.Merchants.Data.Entities;
using SwiftDrop.Merchants.Data.Repositories;
using SwiftDrop.SharedKernel.Authorization;
using SwiftDrop.SharedKernel.Exceptions;
using Xunit;

namespace SwiftDrop.Merchants.UnitTests;

public sealed class OAuthTests
{
    private readonly Mock<IMerchantRepository> _repository = new();
    private readonly Mock<IMerchantEventPublisher> _eventPublisher = new();
    private readonly Mock<IOAuthTokenService> _oauthTokenService = new();
    private readonly RegisterMerchantValidator _registerValidator = new();
    private readonly CreateApiKeyValidator _apiKeyValidator = new();
    private readonly CreateClientCredentialValidator _clientValidator = new();

    private MerchantService CreateService() =>
        new(_repository.Object, _registerValidator, _apiKeyValidator, _clientValidator, _oauthTokenService.Object, _eventPublisher.Object);

    [Fact]
    public async Task CreateClientCredentialAsync_WhenMerchantIsApproved_ShouldCreateCredentials_AndStoreSecretHash()
    {
        // 1. Arrange: Approved merchant
        var merchant = new Merchant("Acme Corp", "PV12345");
        merchant.Approve(Guid.NewGuid());
        var merchantId = merchant.Id;

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();
        var command = new CreateClientCredentialCommand("Production ERP");

        // 2. Act
        var result = await service.CreateClientCredentialAsync(merchantId, command);

        // 3. Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Production ERP");
        result.ClientId.Should().StartWith("sd_client_");
        result.PlainTextClientSecret.Should().StartWith("sd_secret_");

        // Database must store hash, never the raw secret!
        _repository.Verify(r => r.AddClientCredentialAsync(It.Is<ClientCredential>(c =>
            c.MerchantId == merchantId &&
            c.ClientId == result.ClientId &&
            c.ClientSecretHash != result.PlainTextClientSecret &&
            c.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);

        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateClientCredentialAsync_WhenMerchantIsPending_ShouldThrowForbiddenException()
    {
        // Arrange
        var merchant = new Merchant("Pending Shop", "PV55555");
        var merchantId = merchant.Id;

        _repository.Setup(r => r.GetByIdAsync(merchantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();
        var command = new CreateClientCredentialCommand("Pending App");

        // Act & Assert
        var act = () => service.CreateClientCredentialAsync(merchantId, command);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*approved*");
    }

    [Fact]
    public async Task ExchangeClientCredentialsAsync_WithValidCredentialsAndApprovedMerchant_ShouldReturnOAuthToken()
    {
        // Arrange
        var merchant = new Merchant("Global Goods", "PV77777");
        merchant.Approve(Guid.NewGuid());

        var rawSecret = "sd_secret_mysecret1234567890abcdef";
        var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();
        var credential = new ClientCredential(merchant.Id, "Storefront", "sd_client_test123", secretHash);

        _repository.Setup(r => r.GetClientCredentialByClientIdAsync("sd_client_test123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _repository.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var expectedResponse = new OAuthTokenResponse("test.jwt.token", "Bearer", 3600, "merchant");
        _oauthTokenService.Setup(s => s.IssueMerchantToken(merchant.Id, merchant.UserId, merchant.BusinessName, credential.ClientId))
            .Returns(expectedResponse);

        var service = CreateService();
        var request = new OAuthTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "sd_client_test123",
            ClientSecret = rawSecret
        };

        // Act
        var result = await service.ExchangeClientCredentialsAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("test.jwt.token");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(3600);
    }

    [Fact]
    public async Task ExchangeClientCredentialsAsync_WithInvalidGrantType_ShouldThrowBadRequestException()
    {
        var service = CreateService();
        var request = new OAuthTokenRequest
        {
            GrantType = "password", // Unsupported
            ClientId = "sd_client_test",
            ClientSecret = "secret"
        };

        var act = () => service.ExchangeClientCredentialsAsync(request);
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Unsupported grant type*");
    }

    [Fact]
    public async Task ExchangeClientCredentialsAsync_WithWrongSecret_ShouldThrowUnauthorizedException()
    {
        var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("correct_secret"))).ToLowerInvariant();
        var credential = new ClientCredential(Guid.NewGuid(), "App", "sd_client_test", secretHash);

        _repository.Setup(r => r.GetClientCredentialByClientIdAsync("sd_client_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        var service = CreateService();
        var request = new OAuthTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "sd_client_test",
            ClientSecret = "wrong_secret"
        };

        var act = () => service.ExchangeClientCredentialsAsync(request);
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid client credentials*");
    }

    [Fact]
    public async Task ExchangeClientCredentialsAsync_WhenMerchantIsNotApproved_ShouldThrowForbiddenException()
    {
        var merchant = new Merchant("Pending Shop", "PV99999"); // Status is Pending
        var rawSecret = "sd_secret_123";
        var secretHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret))).ToLowerInvariant();
        var credential = new ClientCredential(merchant.Id, "App", "sd_client_test", secretHash);

        _repository.Setup(r => r.GetClientCredentialByClientIdAsync("sd_client_test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _repository.Setup(r => r.GetByIdAsync(merchant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchant);

        var service = CreateService();
        var request = new OAuthTokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "sd_client_test",
            ClientSecret = rawSecret
        };

        var act = () => service.ExchangeClientCredentialsAsync(request);
        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*not active or approved*");
    }

    [Fact]
    public void OAuthTokenService_IssueMerchantToken_ShouldProduceValidToken_WithMerchantClaims()
    {
        // Test token issuer service directly
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "SwiftDrop",
            Audience = "SwiftDrop.Client",
            Key = "SuperSecretKeyForTestingTokensThatIsAtLeast32BytesLong!",
            ExpiryMinutes = 60
        });

        var tokenService = new OAuthTokenService(jwtOptions);
        var merchantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var response = tokenService.IssueMerchantToken(merchantId, userId, "Nike Store", "sd_client_nike");

        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.TokenType.Should().Be("Bearer");
        response.ExpiresIn.Should().Be(3600);
    }
}

