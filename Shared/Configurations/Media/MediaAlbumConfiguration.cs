using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Media;

public class MediaAlbumConfiguration
    : IEntityTypeConfiguration<MediaAlbum>
{
    public void Configure(EntityTypeBuilder<MediaAlbum> builder)
    {
        // =========================
        // Table
        // =========================

        builder.ToTable("MediaAlbums");


        // =========================
        // Primary Key
        // =========================

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();


        // =========================
        // Basic information
        // =========================

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Description)
            .HasMaxLength(500);


        // =========================
        // Audit
        // =========================

        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt);


        // =========================
        // Relationships
        // =========================


        // =========================
        // Indexes
        // =========================

        builder.HasIndex(x => x.Name);
    }
}