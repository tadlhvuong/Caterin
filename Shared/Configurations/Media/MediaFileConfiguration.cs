using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Media;

namespace Shared.Configurations.Media
{
    public class MediaFileConfiguration : IEntityTypeConfiguration<MediaFile>
    {
        public void Configure(EntityTypeBuilder<MediaFile> builder)
        {
            builder.ToTable("MediaFiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.FileName).IsRequired().HasMaxLength(255);

            builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(500);

            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);

            builder.Property(x => x.Size).IsRequired();

            builder.Property(x => x.OriginalFileName).HasMaxLength(255);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();

            builder.HasMany(x => x.ProductMedias).WithOne(x => x.MediaFile)
                .HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.Restrict);

            builder.HasMany(x => x.ProductVariantMedias).WithOne(x => x.MediaFile)
                .HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}