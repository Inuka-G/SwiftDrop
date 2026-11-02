using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Identity.Data.Entities;

public class User : BaseEntity
{
    public const string LocalProvider = "LOCAL";
    public const string GoogleProvider = "GOOGLE";

    private User() { }   // required by EF Core

    public User(string fullName, string email, string? username, string? phoneNumber, Role role)
    {
        FullName = fullName;
        Email = email;
        Username = username;
        PhoneNumber = phoneNumber;
        Role = role;
        RoleId = role.Id;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public string FullName { get; private set; } = default!;
    public string Email { get; private set; } = default!;

    /// Optional short login name, such as "admin". Signed-up users log in with their email.
    public string? Username { get; private set; }
    public string? PhoneNumber { get; private set; }

    /// Null for Google users, who have no password here.
    public string? PasswordHash { get; private set; }
    public string AuthProvider { get; private set; } = LocalProvider;

    /// Google's id for this person; null for local users.
    public string? ProviderUserId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime UpdatedAtUtc { get; private set; }

    public int RoleId { get; private set; }
    public Role Role { get; private set; } = default!;

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
