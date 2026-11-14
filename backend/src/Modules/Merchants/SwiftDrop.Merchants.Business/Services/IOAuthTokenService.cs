using SwiftDrop.Merchants.Business.Dtos;

namespace SwiftDrop.Merchants.Business.Services;

public interface IOAuthTokenService
{
    OAuthTokenResponse IssueMerchantToken(Guid merchantId, Guid? userId, string? businessName, string clientId);
}

