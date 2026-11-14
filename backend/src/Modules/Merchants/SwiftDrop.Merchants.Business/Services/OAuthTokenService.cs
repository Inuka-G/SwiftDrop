using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SwiftDrop.Merchants.Business.Dtos;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Merchants.Business.Services;

internal sealed class OAuthTokenService(IOptions<JwtOptions> options) : IOAuthTokenService
{
    public OAuthTokenResponse IssueMerchantToken(Guid merchantId, Guid? userId, string? businessName, string clientId)
    {
        var jwt = options.Value;
        var expiresInSeconds = jwt.ExpiryMinutes > 0 ? jwt.ExpiryMinutes * 60 : 3600;
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);
        var keyBytes = Encoding.UTF8.GetBytes(string.IsNullOrEmpty(jwt.Key) ? "SwiftDrop-Default-Secret-Key-For-OAuth-Testing-Purposes-Only-32chars" : jwt.Key);
        var key = new SymmetricSecurityKey(keyBytes);

        var subValue = (userId ?? merchantId).ToString();

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = string.IsNullOrEmpty(jwt.Issuer) ? "SwiftDrop" : jwt.Issuer,
            Audience = string.IsNullOrEmpty(jwt.Audience) ? "SwiftDrop.Client" : jwt.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", subValue),
                new Claim("merchant_id", merchantId.ToString()),
                new Claim("name", businessName ?? "Merchant"),
                new Claim("role", Roles.Merchant),
                new Claim("client_id", clientId),
            ]),
        });

        return new OAuthTokenResponse(
            AccessToken: token,
            TokenType: "Bearer",
            ExpiresIn: expiresInSeconds,
            Scope: "merchant");
    }
}

