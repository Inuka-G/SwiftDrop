using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Merchants.Data.Entities;

public class ApiKey : BaseEntity
{
    private ApiKey() { } // Required by Entity Framework

    public ApiKey(Guid merchantId, string name, string keyPrefix, string keyHash)
    {
        MerchantId = merchantId;
        Name = name.Trim();
        KeyPrefix = keyPrefix;
        KeyHash = keyHash;
        IsActive = true;
    }

    public Guid MerchantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string KeyPrefix { get; private set; } = default!;
    public string KeyHash { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime? RevokedAtUtc { get; private set; }

    public void Revoke()
    {
        IsActive = false;
        RevokedAtUtc = DateTime.UtcNow;
    }
}

