using SwiftDrop.Identity.Data.Entities;

namespace SwiftDrop.Identity.Business.Services;

public interface ITokenService
{
    AccessToken CreateToken(User user);
}
