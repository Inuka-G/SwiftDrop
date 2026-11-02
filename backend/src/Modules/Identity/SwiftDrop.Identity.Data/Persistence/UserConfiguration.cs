using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Identity.Data.Entities;

namespace SwiftDrop.Identity.Data.Persistence;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(u => u.Id);
        b.Property(u => u.FullName).HasMaxLength(100).IsRequired();
        b.Property(u => u.Email).HasMaxLength(254).IsRequired();
        b.HasIndex(u => u.Email).IsUnique();
        b.Property(u => u.Username).HasMaxLength(50);
        b.HasIndex(u => u.Username).IsUnique();
        b.Property(u => u.PhoneNumber).HasMaxLength(20);
        b.Property(u => u.AuthProvider).HasMaxLength(20).IsRequired();
        b.Property(u => u.ProviderUserId).HasMaxLength(255);
        b.HasOne(u => u.Role).WithMany().HasForeignKey(u => u.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
