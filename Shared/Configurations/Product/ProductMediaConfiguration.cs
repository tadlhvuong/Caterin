using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;

namespace Shared.Configurations.Product
{
    public class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
    {
        public void Configure(EntityTypeBuilder<ProductMedia> builder)
        {
            builder.ToTable("ProductMedias");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId).IsRequired();

            builder.Property(x => x.MediaFileId).IsRequired();

            builder.Property(x => x.IsPrimary).IsRequired();

            builder.Property(x => x.DisplayOrder).IsRequired();

            builder.HasOne(x => x.Product).WithMany(x => x.ProductMedias)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.MediaFile).WithMany(x => x.ProductMedias)
                .HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.ProductId,
                x.DisplayOrder
            });

            builder.HasIndex(x => new
            {
                x.ProductId,
                x.IsPrimary
            });
        }
    }
}