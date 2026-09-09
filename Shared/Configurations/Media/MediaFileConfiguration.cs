using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Media;

public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.ToTable("MediaFiles");

        // =====================================================
        // Primary Key
        // =====================================================

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        // =====================================================
        // FileName
        // =====================================================

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(255);

        // =====================================================
        // StoragePath
        // =====================================================

        builder.Property(x => x.StoragePath)
            .IsRequired()
            .HasMaxLength(500);

        // =====================================================
        // ContentType
        // =====================================================

        builder.Property(x => x.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        // =====================================================
        // Size
        // =====================================================

        builder.Property(x => x.Size)
            .IsRequired();

        // =====================================================
        // OriginalFileName
        // =====================================================

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(255);

        // =====================================================
        // CreatedAt
        // =====================================================

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        // =====================================================
        // ProductMedia
        // MediaFile 1:N ProductMedia
        // =====================================================

        builder.HasMany(x => x.ProductMedias)
            .WithOne(x => x.MediaFile)
            .HasForeignKey(x => x.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.ProductVariantMedias)
            .WithOne(x => x.MediaFile)
            .HasForeignKey(x => x.MediaFileId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}