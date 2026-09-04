using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

public class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        // Primary Key
        builder.HasKey(x => x.Id);

        // OrderId
        builder.Property(x => x.OrderId)
            .IsRequired();

        // ProductVariantId
        builder.Property(x => x.ProductVariantId)
            .IsRequired();

        // ProductName
        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(250);

        // VariantName
        builder.Property(x => x.VariantName)
            .IsRequired()
            .HasMaxLength(200);

        // Price
        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        // Quantity
        builder.Property(x => x.Quantity)
            .IsRequired();

        // Total
        builder.Property(x => x.Total)
            .IsRequired()
            .HasPrecision(18, 2);

        // CreatedAt
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // Order relationship
        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ProductVariant relationship
        builder.HasOne(x => x.ProductVariants)
            .WithMany()
            .HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Warehouse)
    .WithMany(x => x.OrderItems)
    .HasForeignKey(x => x.WarehouseId)
    .OnDelete(DeleteBehavior.Restrict);
        // Index
        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => x.ProductVariantId);
    }
}