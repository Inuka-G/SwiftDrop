using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SwiftDrop.Identity.Data.Entities;
using SwiftDrop.SharedKernel.Authorization;

namespace SwiftDrop.Identity.Data.Persistence;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Name).HasMaxLength(20).IsRequired();
        b.HasIndex(r => r.Name).IsUnique();

        // Roles are fixed reference data, needed in every environment.
        b.HasData(
            new Role(1, Roles.Customer),
            new Role(2, Roles.Merchant),
            new Role(3, Roles.Courier),
            new Role(4, Roles.Dispatcher),
            new Role(5, Roles.Admin));
    }
}
