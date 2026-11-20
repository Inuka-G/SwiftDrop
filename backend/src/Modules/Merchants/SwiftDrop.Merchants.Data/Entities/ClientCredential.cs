using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Merchants.Data.Entities;

public class ClientCredential : BaseEntity
{
    private ClientCredential() { } // Required by Entity Framework

    public ClientCredential(Guid merchantId, string name, string clientId, string clientSecretHash)
    {
        MerchantId = merchantId;
        Name = name.Trim();
        ClientId = clientId;
        ClientSecretHash = clientSecretHash;
        IsActive = true;
    }

    public Guid MerchantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string ClientId { get; private set; } = default!;
    public string ClientSecretHash { get; private set; } = default!;
    public bool IsActive { get; private set; } = true;
    public DateTime? RevokedAtUtc { get; private set; }

    public void Revoke()
    {
        IsActive = false;
        RevokedAtUtc = DateTime.UtcNow;
    }
}

