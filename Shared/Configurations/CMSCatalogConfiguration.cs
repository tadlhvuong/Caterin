using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations
{
    public class CMSCatalogConfiguration : IEntityTypeConfiguration<CMSCatalog>
    {
        public void Configure(EntityTypeBuilder<CMSCatalog> builder)
        {
            builder.ToTable("CMSCatalogs");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(50);

            builder.Property(x => x.Name).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Description).HasMaxLength(255);

            builder.Property(x => x.Type).IsRequired();

            builder.Property(x => x.IsActive).HasDefaultValue(true);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");

            builder.HasOne(x => x.Parent).WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);

            // ===== Index =====

            builder.HasIndex(x => x.Type);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name);
        }
    }
}
