using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BannerEntity = Shared.Data.Entities.Marketing.Banner;

namespace Shared.Configurations.Banner
{
    public class BannerConfiguration : IEntityTypeConfiguration<BannerEntity>
    {
        public void Configure(EntityTypeBuilder<BannerEntity> builder)
        {
            builder.ToTable("banners");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.StartAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.EndAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("timestamp with time zone");

            builder.Property(x => x.DeletedAt)
                .HasColumnType("timestamp with time zone");

            // Banner - BannerMedia
            builder.HasMany(x => x.BannerMedias)
                .WithOne(x => x.Banner)
                .HasForeignKey(x => x.BannerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index phục vụ truy vấn Banner
            builder.HasIndex(x => new
            {
                x.Position,
                x.IsPublished,
                x.IsDeleted,
                x.SortOrder
            });
        }
    }
}
