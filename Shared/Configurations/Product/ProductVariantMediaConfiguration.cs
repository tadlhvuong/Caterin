using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations.Product
{
    public class ProductVariantMediaConfiguration
    : IEntityTypeConfiguration<ProductVariantMedia>
    {
        public void Configure(EntityTypeBuilder<ProductVariantMedia> builder)
        {
            builder.ToTable("ProductVariantMedias");

            builder.HasKey(x => x.Id);

            // =====================================================
            // ProductVariant
            // =====================================================

            builder.Property(x => x.ProductVariantId)
                .IsRequired();

            builder.HasOne(x => x.ProductVariant)
                .WithMany(x => x.VariantMedias)
                .HasForeignKey(x => x.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // =====================================================
            // AttributeValue
            // =====================================================

            builder.Property(x => x.AttributeValueId)
                .IsRequired();

            builder.HasOne(x => x.AttributeValue)
                .WithMany(x => x.ProductVariantMedias)
                .HasForeignKey(x => x.AttributeValueId)
                .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // MediaFile
            // =====================================================

            builder.Property(x => x.MediaFileId)
                .IsRequired();

            builder.HasOne(x => x.MediaFile)
    .WithMany(x => x.ProductVariantMedias)
    .HasForeignKey(x => x.MediaFileId)
    .OnDelete(DeleteBehavior.Restrict);

            // =====================================================
            // Display
            // =====================================================

            builder.Property(x => x.DisplayOrder)
                .IsRequired();

            // =====================================================
            // Prevent duplicate media for same variant + option
            // =====================================================

            builder.HasIndex(x => new
            {
                x.ProductVariantId,
                x.AttributeValueId,
                x.MediaFileId
            })
            .IsUnique();
        }
    }
}
