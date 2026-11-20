using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Parcels.Data.Entities;

namespace SwiftDrop.Parcels.Data.Persistence;

internal sealed class ParcelConfiguration : IEntityTypeConfiguration<Parcel>
{
    public void Configure(EntityTypeBuilder<Parcel> b)
    {
        b.ToTable("parcels");
        b.HasKey(p => p.Id);

        // Tracking Number: unique and indexed for fast public tracking
        b.Property(p => p.TrackingNumber).HasMaxLength(32).IsRequired();
        b.HasIndex(p => p.TrackingNumber).IsUnique();

        // Customer & Zone IDs
        b.Property(p => p.CustomerId).IsRequired();
        b.HasIndex(p => p.CustomerId);
        b.Property(p => p.ZoneId).IsRequired();

        // Addresses & Contacts
        b.Property(p => p.PickupAddress).HasMaxLength(500).IsRequired();
        b.Property(p => p.PickupContactName).HasMaxLength(150).IsRequired();
        b.Property(p => p.PickupContactPhone).HasMaxLength(30).IsRequired();

        b.Property(p => p.DropoffAddress).HasMaxLength(500).IsRequired();
        b.Property(p => p.RecipientName).HasMaxLength(150).IsRequired();
        b.Property(p => p.RecipientPhone).HasMaxLength(30).IsRequired();

        // Financials & Specs
        b.Property(p => p.WeightKg).HasPrecision(8, 2);
        b.Property(p => p.SizeCategory).HasMaxLength(50).IsRequired();
        b.Property(p => p.EstimatedFee).HasPrecision(10, 2);
        b.Property(p => p.FinalFee).HasPrecision(10, 2);

        // Status
        b.Property(p => p.CurrentStatus).HasMaxLength(50).IsRequired();
        b.Property(p => p.PaymentMethod).HasMaxLength(50).IsRequired();
        b.Property(p => p.PaymentStatus).HasMaxLength(50).IsRequired();

        // Courier assignment: indexed for the courier's daily delivery list
        b.HasIndex(p => new { p.CourierId, p.CurrentStatus });

        // Concurrency token
        b.Property(p => p.Version).IsConcurrencyToken();
    }
}