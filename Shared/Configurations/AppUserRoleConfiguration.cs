using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity;

public class AppUserRoleConfiguration : IEntityTypeConfiguration<AppUserRole>
{
    public void Configure(EntityTypeBuilder<AppUserRole> builder)
    {
        builder.ToTable("UserRoles");

        builder.HasKey(x => new { x.UserId, x.RoleId });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);

        builder.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
    }
}