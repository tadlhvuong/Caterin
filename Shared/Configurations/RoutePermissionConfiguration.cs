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
    public sealed class RoutePermissionConfiguration
    : IEntityTypeConfiguration<RoutePermission>
    {
        public void Configure(EntityTypeBuilder<RoutePermission> builder)
        {
            builder.ToTable("RoutePermissions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Route)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.HttpMethod)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(x => x.Controller)
                .HasMaxLength(100);

            builder.Property(x => x.Action)
                .HasMaxLength(100);

            builder.Property(x => x.PermissionId)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt);

            builder.HasOne(x => x.Permission)
                .WithMany(x => x.RoutePermissions)
                .HasForeignKey(x => x.PermissionId)
                .OnDelete(DeleteBehavior.Restrict);

            // 1 route + 1 method chỉ được map 1 permission
            builder.HasIndex(x => new
            {
                x.Route,
                x.HttpMethod
            })
            .IsUnique();

            builder.HasIndex(x => x.PermissionId);

            builder.HasIndex(x => new
            {
                x.Controller,
                x.Action
            });
        }
    }
}
