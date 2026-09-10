using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

namespace Shared.Configurations.Order
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.OrderId).IsRequired();

            builder.Property(x => x.ProductVariantId).IsRequired();

            builder.Property(x => x.ProductName).IsRequired().HasMaxLength(250);

            builder.Property(x => x.VariantName).IsRequired().HasMaxLength(200);

            builder.Property(x => x.Price).IsRequired().HasPrecision(18, 2);

            builder.Property(x => x.Quantity).IsRequired();

            builder.Property(x => x.Total).IsRequired().HasPrecision(18, 2);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.HasOne(x => x.Order).WithMany(x => x.Items)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ProductVariants).WithMany()
                .HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Warehouse).WithMany(x => x.OrderItems)
                .HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            
            builder.HasIndex(x => x.OrderId);

            builder.HasIndex(x => x.ProductVariantId);
        }
    }
}