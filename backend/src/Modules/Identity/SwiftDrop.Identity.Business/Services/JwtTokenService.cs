using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Business.Services;

/// Creates the signed token a user sends with each request. The signature proves the
/// server issued it, so the role inside cannot be changed without the secret key.
internal sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    public AccessToken CreateToken(User user)
    {
        var jwt = options.Value;
        var expiresAt = DateTime.UtcNow.AddMinutes(jwt.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));

        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim("sub", user.Id.ToString()),
                new Claim("name", user.FullName),
                new Claim("email", user.Email),
                new Claim("role", user.Role.Name),
            ]),
        });

        return new AccessToken(token, expiresAt);
    }
}
