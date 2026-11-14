using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Data.Persistence;

internal sealed class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> builder)
    {
        builder.ToTable("merchants");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessName)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.BusinessRegNumber)
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasMaxLength(24)
            .IsRequired();

        builder.Property(x => x.WebhookUrl)
            .HasMaxLength(500);

        builder.Property(x => x.WebhookSecret)
            .HasMaxLength(128);

        builder.HasIndex(x => x.BusinessRegNumber)
            .IsUnique();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.UserId);
    }
}

