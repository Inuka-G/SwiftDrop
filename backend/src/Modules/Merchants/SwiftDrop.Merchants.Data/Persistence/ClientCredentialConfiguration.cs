using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Merchants.Data.Entities;

namespace SwiftDrop.Merchants.Data.Persistence;

internal sealed class ClientCredentialConfiguration : IEntityTypeConfiguration<ClientCredential>
{
    public void Configure(EntityTypeBuilder<ClientCredential> builder)
    {
        builder.ToTable("client_credentials");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ClientId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.ClientSecretHash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.HasIndex(x => x.ClientId)
            .IsUnique();

        builder.HasIndex(x => x.MerchantId);
    }
}

