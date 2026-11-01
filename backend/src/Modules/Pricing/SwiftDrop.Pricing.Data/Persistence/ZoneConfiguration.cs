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
        b.Property(z => z.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(z => z.Name).IsUnique();
        b.Property(z => z.BaseFee).HasPrecision(10, 2);
        b.Property(z => z.PerKgRate).HasPrecision(10, 2);
    }
}