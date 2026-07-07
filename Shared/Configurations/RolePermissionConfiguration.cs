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
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermissions");

            builder.HasKey(x => new
            {
                x.RoleId,
                x.PermissionId
            });

            builder.Property(x => x.RoleId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.PermissionId)
                .IsRequired();

            builder.Property(x => x.AssignedById)
                .HasMaxLength(450);

            builder.Property(x => x.AssignedAt)
                .HasColumnType("timestamp with time zone");

            builder.HasOne(x => x.Role)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Permission)
                .WithMany(x => x.RolePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.AssignedByUser)
                .WithMany()
                .HasForeignKey(x => x.AssignedById)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(x => x.RoleId);

            builder.HasIndex(x => x.PermissionId);

            builder.HasIndex(x => x.AssignedById);
        }
    }
}
