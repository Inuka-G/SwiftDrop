namespace SwiftDrop.SharedKernel.Authorization;

/// The role names stored in the roles table and placed in the JWT "role" claim.
/// Usage on a controller or action: [Authorize(Roles = Roles.Courier)]
public static class Roles
{
    public const string Customer = "CUSTOMER";
    public const string Merchant = "MERCHANT";
    public const string Courier = "COURIER";
    public const string Dispatcher = "DISPATCHER";
    public const string Admin = "ADMIN";

    /// A comma means "any of these roles" inside [Authorize(Roles = ...)].
    public const string DispatcherOrAdmin = Dispatcher + "," + Admin;
}
