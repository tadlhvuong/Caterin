using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

namespace Shared.Configurations.Product
{
    public class ProductVariantMediaConfiguration : IEntityTypeConfiguration<ProductVariantMedia>
    {
        public void Configure(EntityTypeBuilder<ProductVariantMedia> builder)
        {
            builder.ToTable("ProductVariantMedias");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductVariantId).IsRequired();

            builder.HasOne(x => x.ProductVariant).WithMany(x => x.VariantMedias)
                .HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.AttributeValueId).IsRequired();

            builder.HasOne(x => x.AttributeValue).WithMany(x => x.ProductVariantMedias)
                .HasForeignKey(x => x.AttributeValueId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.MediaFileId).IsRequired();

            builder.HasOne(x => x.MediaFile).WithMany(x => x.ProductVariantMedias)
                .HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.DisplayOrder).IsRequired();

            builder.HasIndex(x => new
            {
                x.ProductVariantId,
                x.AttributeValueId,
                x.MediaFileId
            }).IsUnique();
        }
    }
}
