using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Code).IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Code)
                .IsUnique();

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");

            builder.HasMany(x => x.RolePermissions)
                .WithOne(x => x.Permission)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class CMSModuleConfig : IEntityTypeConfiguration<CMSModule>
    {
        public void Configure(EntityTypeBuilder<CMSModule> builder)
        {
            builder.ToTable("CMSModule");
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Code)
                .HasMaxLength(50);

            builder.Property(x => x.Name)
                .HasMaxLength(100);
        }
    }

    public class PermissionLogConfig : IEntityTypeConfiguration<PermissionLog>
    {
        public void Configure(EntityTypeBuilder<PermissionLog> builder)
        {
            builder.ToTable("PermissionLogs");

            // PK
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            // ChangeBy
            builder.Property(x => x.ChangeBy)
                .IsRequired()
                .HasMaxLength(100);

            // RoleId
            builder.Property(x => x.RoleId)
                .IsRequired();

            builder.HasIndex(x => x.RoleId);

            // PermissionOld
            builder.Property(x => x.PermissionOld)
                .IsRequired()
                .HasMaxLength(2000);

            // PermissionNew
            builder.Property(x => x.PermissionNew)
                .IsRequired()
                .HasMaxLength(2000);

            // CreatedAt
            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone");

            // Relationship Role
            builder.HasOne(x => x.Role)
                .WithMany() // nếu AppRole chưa có navigation logs
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

}
