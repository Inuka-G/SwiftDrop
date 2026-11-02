using System.Security.Claims;

namespace SwiftDrop.SharedKernel.Authorization;

public static class ClaimsPrincipalExtensions
{
    /// The logged-in user's id, taken from the JWT "sub" claim.
    /// Use it for ownership checks, such as "is this parcel assigned to this courier?".
    public static Guid? GetUserId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue("sub"), out var id) ? id : null;
}
