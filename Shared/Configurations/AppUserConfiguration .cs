using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity;

namespace Shared.Configurations
{
    public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PermissionVersion).HasDefaultValue(1);

            builder.Property(x => x.UserName).HasMaxLength(256);

            builder.Property(x => x.Email).HasMaxLength(256);

            builder.Property(x => x.UserName).HasMaxLength(200);

            builder.Property(x => x.AddressDetail).HasMaxLength(500);

            builder.Property(x => x.ProvinceCode).HasMaxLength(20);

            builder.Property(x => x.WardCode).HasMaxLength(20);

            builder.Property(x => x.DateOfBirth).IsRequired(false).HasColumnType("timestamp with time zone");

            builder.Property(x => x.CreatedAt).IsRequired(true).HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt).IsRequired(false).HasColumnType("timestamp with time zone");

            builder.Property(x => x.LastLogin).IsRequired(false).HasColumnType("timestamp with time zone");

            builder.HasOne(u => u.AvatarMediaFile).WithMany()
                .HasForeignKey(u => u.AvatarMediaFileId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}