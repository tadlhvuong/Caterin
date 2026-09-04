using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

public class OrderAddressConfiguration : IEntityTypeConfiguration<OrderAddress>
{
    public void Configure(EntityTypeBuilder<OrderAddress> builder)
    {
        // =========================
        // Table
        // =========================

        builder.ToTable("OrderAddresses");

        // =========================
        // Primary Key
        // =========================

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        // =========================
        // OrderId
        // =========================

        builder.Property(x => x.OrderId)
            .IsRequired();

        // =========================
        // Receiver
        // =========================

        builder.Property(x => x.ReceiverName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(20);

        // =========================
        // Address
        // =========================

        builder.Property(x => x.Province)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.District)
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(x => x.Ward)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.AddressLine)
            .IsRequired()
            .HasMaxLength(500);

        // =========================
        // Order relationship
        // =========================

        builder.HasOne(x => x.Order)
            .WithOne(x => x.Address)
            .HasForeignKey<OrderAddress>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Index
        // =========================

        builder.HasIndex(x => x.OrderId)
            .IsUnique();
    }
}