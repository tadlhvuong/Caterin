using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Product;
using Shared.Enums;

namespace Shared.Configurations.Product
{
    public class ProductTagMappingConfiguration : IEntityTypeConfiguration<ProductTagMapping>
    {
        public void Configure(EntityTypeBuilder<ProductTagMapping> builder)
        {
            builder.ToTable("ProductTagMappings");

            builder.HasKey(x => new
            {
                x.ProductId,
                x.TagId
            });

            builder.HasOne(x => x.Product).WithMany(x => x.ProductTagMappings)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Tag).WithMany(x => x.ProductTagMappings)
                .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.DisplayOrder).HasDefaultValue(0);

            builder.HasIndex(x => x.TagId);
        }
    }
}