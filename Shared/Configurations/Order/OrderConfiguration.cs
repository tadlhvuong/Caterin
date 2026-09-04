
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // =========================
        // Table
        // =========================

        builder.ToTable("Orders");

        // =========================
        // Primary Key
        // =========================

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        // =========================
        // UserId
        // =========================

        builder.Property(x => x.UserId)
            .IsRequired()
            .HasMaxLength(450);

        // =========================
        // OrderCode
        // =========================

        builder.Property(x => x.OrderCode)
            .IsRequired()
            .HasMaxLength(50);

        // OrderCode phải unique
        builder.HasIndex(x => x.OrderCode)
            .IsUnique();

        // =========================
        // Status
        // =========================

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        // =========================
        // Money
        // =========================

        builder.Property(x => x.SubTotal)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.DiscountAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.ShippingAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(x => x.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        // =========================
        // Note
        // =========================

        builder.Property(x => x.Note)
            .HasMaxLength(1000)
            .IsRequired(false);

        // =========================
        // DateTime
        // =========================

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .IsRequired();

        // =========================
        // Order -> User
        // =========================

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // =========================
        // Order -> Address
        // =========================

        builder.HasOne(x => x.Address)
            .WithOne(x => x.Order)
            .HasForeignKey<OrderAddress>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Order -> Items
        // =========================

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Order -> Histories
        // =========================

        builder.HasMany(x => x.Histories)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Indexes
        // =========================

        builder.HasIndex(x => x.UserId);

        builder.HasIndex(x => x.Status);

        builder.HasIndex(x => x.CreatedAt);
    }
}