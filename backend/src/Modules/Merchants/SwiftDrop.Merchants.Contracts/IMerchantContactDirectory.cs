namespace SwiftDrop.Merchants.Contracts;

public interface IMerchantContactDirectory
{
    Task<Guid?> GetApprovedContactUserIdAsync(Guid id, CancellationToken ct);
}
