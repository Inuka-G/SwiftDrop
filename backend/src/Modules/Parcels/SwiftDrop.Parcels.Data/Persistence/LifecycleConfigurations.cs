using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Parcels.Data.Entities;

namespace SwiftDrop.Parcels.Data.Persistence;

internal sealed class ParcelStatusHistoryConfiguration : IEntityTypeConfiguration<ParcelStatusHistory>
{
    public void Configure(EntityTypeBuilder<ParcelStatusHistory> b)
    {
        b.ToTable("status_history");
        b.HasKey(h => h.Id);
        b.HasOne<Parcel>().WithMany().HasForeignKey(h => h.ParcelId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(h => new { h.ParcelId, h.CreatedAtUtc });

        b.Property(h => h.FromStatus).HasMaxLength(50);
        b.Property(h => h.ToStatus).HasMaxLength(50).IsRequired();
        b.Property(h => h.Note).HasMaxLength(500);
        b.Property(h => h.ChangedByRole).HasMaxLength(50).IsRequired();
    }
}

internal sealed class DeliveryProofConfiguration : IEntityTypeConfiguration<DeliveryProof>
{
    public void Configure(EntityTypeBuilder<DeliveryProof> b)
    {
        b.ToTable("delivery_proofs");
        b.HasKey(p => p.Id);
        b.HasOne<Parcel>().WithMany().HasForeignKey(p => p.ParcelId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(p => p.ParcelId).IsUnique();   // a parcel is delivered once

        b.Property(p => p.RecipientName).HasMaxLength(150);
        b.Property(p => p.Note).HasMaxLength(500);
        b.Property(p => p.PhotoKey).HasMaxLength(100);
        b.Property(p => p.PhotoContentType).HasMaxLength(50);
    }
}

internal sealed class FailureReportConfiguration : IEntityTypeConfiguration<FailureReport>
{
    public void Configure(EntityTypeBuilder<FailureReport> b)
    {
        b.ToTable("failure_reports");
        b.HasKey(f => f.Id);
        b.HasOne<Parcel>().WithMany().HasForeignKey(f => f.ParcelId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(f => f.ParcelId);

        b.Property(f => f.Reason).HasMaxLength(50).IsRequired();
        b.Property(f => f.Note).HasMaxLength(500);
        b.Property(f => f.PhotoKey).HasMaxLength(100);
        b.Property(f => f.PhotoContentType).HasMaxLength(50);
    }
}

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(m => m.Id);
        b.HasIndex(m => new { m.ProcessedAtUtc, m.CreatedAtUtc });

        b.Property(m => m.RoutingKey).HasMaxLength(100).IsRequired();
        b.Property(m => m.Payload).IsRequired();
        b.Property(m => m.LastError).HasMaxLength(OutboxMessage.MaxErrorLength);
    }
}
