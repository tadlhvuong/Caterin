using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity;

public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(256).IsRequired();

        builder.Property(x => x.Level).IsRequired();

        builder.Property(x => x.IsSystem).HasDefaultValue(false);

        builder.Property(x => x.Description).HasMaxLength(500);

        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasMany(x => x.RolePermissions).WithOne(x => x.Role).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
    }
}