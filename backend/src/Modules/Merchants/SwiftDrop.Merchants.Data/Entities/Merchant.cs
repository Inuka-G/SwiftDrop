using SwiftDrop.SharedKernel.Abstractions;

namespace SwiftDrop.Merchants.Data.Entities;

public static class MerchantStatus
{
    public const string Pending = "Pending";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Suspended = "Suspended";
}

public static class MerchantRegType
{
    public const string Brn = "BRN";
    public const string Crn = "CRN";
    public const string Tin = "TIN";
}

public class Merchant : BaseEntity
{
    private Merchant() { } // Required by Entity Framework

    public Merchant(
        string businessName,
        string businessRegNumber,
        Guid? userId = null,
        string? webhookUrl = null,
        string businessRegType = MerchantRegType.Brn)
    {
        BusinessName = businessName.Trim();
        BusinessRegType = string.IsNullOrWhiteSpace(businessRegType)
            ? MerchantRegType.Brn
            : businessRegType.Trim().ToUpperInvariant();
        // Trim leading/trailing whitespace while preserving leading zeros and letters
        BusinessRegNumber = businessRegNumber.Trim();
        UserId = userId;
        WebhookUrl = webhookUrl;
        Status = MerchantStatus.Pending;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public string BusinessName { get; private set; } = default!;
    public string BusinessRegType { get; private set; } = MerchantRegType.Brn;
    public string BusinessRegNumber { get; private set; } = default!;
    public string Status { get; private set; } = MerchantStatus.Pending;
    public string? WebhookUrl { get; private set; }
    public string? WebhookSecret { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    public void Approve(Guid approvedBy)
    {
        Status = MerchantStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reject(Guid adminId)
    {
        Status = MerchantStatus.Rejected;
        ApprovedBy = adminId;
        ApprovedAt = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = MerchantStatus.Suspended;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateWebhook(string webhookUrl, string? secret)
    {
        WebhookUrl = webhookUrl;
        WebhookSecret = secret;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdateProfile(
        string businessName,
        string businessRegType,
        string businessRegNumber,
        string? webhookUrl)
    {
        BusinessName = businessName.Trim();
        BusinessRegType = string.IsNullOrWhiteSpace(businessRegType)
            ? MerchantRegType.Brn
            : businessRegType.Trim().ToUpperInvariant();
        BusinessRegNumber = businessRegNumber.Trim();
        WebhookUrl = string.IsNullOrWhiteSpace(webhookUrl) ? null : webhookUrl.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
