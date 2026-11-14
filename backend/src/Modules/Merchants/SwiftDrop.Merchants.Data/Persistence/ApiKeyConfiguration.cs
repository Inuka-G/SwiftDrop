using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Data.Persistence;

internal sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.KeyPrefix)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.KeyHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.KeyHash)
            .IsUnique();

        builder.HasIndex(x => x.MerchantId);
    }
}

