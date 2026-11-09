using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Pricing.Data.Entities;

namespace SwiftDrop.Pricing.Data.Persistence;

internal sealed class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> b)
    {
        b.ToTable("zones");
        b.HasKey(z => z.Id);
        b.Property(z => z.ZoneName).HasMaxLength(100).IsRequired();
        b.HasIndex(z => z.ZoneName).IsUnique();
        b.Property(z=> z.Description).HasMaxLength(500);
        b.Property(z=> z.IsActive).IsRequired();
    }
}