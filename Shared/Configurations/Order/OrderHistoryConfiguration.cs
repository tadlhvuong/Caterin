using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Order;

public class OrderHistoryConfiguration
    : IEntityTypeConfiguration<OrderHistory>
{
    public void Configure(EntityTypeBuilder<OrderHistory> builder)
    {
        // =========================
        // Table
        // =========================

        builder.ToTable("OrderHistories");

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
        // Status
        // =========================

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        // =========================
        // Note
        // =========================

        builder.Property(x => x.Note)
            .HasMaxLength(1000)
            .IsRequired(false);

        // =========================
        // CreatedAt
        // =========================

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // =========================
        // Relationship
        // =========================

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Histories)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // =========================
        // Index
        // =========================

        builder.HasIndex(x => x.OrderId);

        builder.HasIndex(x => new
        {
            x.OrderId,
            x.CreatedAt
        });
    }
}