namespace SwiftDrop.Identity.Business.Services;

public sealed record UserAdministrationDto(Guid Id, string FullName, string Email, string Role,
    bool IsActive, string AuthProvider, DateTime CreatedAtUtc);

public sealed record UserAdministrationPage(IReadOnlyList<UserAdministrationDto> Items, int Page,
    int PageSize, int TotalCount);

public sealed record UserAdministrationQuery(string? Search, string? Role, bool? IsActive, int Page, int PageSize);

public enum UserAdministrationFailure
{
    UserNotFound,
    InvalidRole,
    SelfChange,
    LastActiveAdmin,
    MerchantRoleRestricted
}

public sealed record UserAdministrationResult<T>(T? Value, UserAdministrationFailure? Failure) where T : class
{
    public bool IsSuccess => Failure is null;
    public static UserAdministrationResult<T> Success(T value) => new(value, null);
    public static UserAdministrationResult<T> Fail(UserAdministrationFailure failure) => new(null, failure);
}

public static class UserAdministrationLimits
{
    public const int MaximumPageSize = 100;
    public const int DefaultPageSize = 20;
    public const int MaximumSearchLength = 254;
}

