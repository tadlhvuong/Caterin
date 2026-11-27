using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations.Banner
{
    public class BannerMediaConfiguration
    : IEntityTypeConfiguration<BannerMedia>
    {
        public void Configure(EntityTypeBuilder<BannerMedia> builder)
        {
            builder.ToTable("banner_media");

            builder.HasKey(x => x.Id);

            builder.HasOne(x => x.Banner)
                .WithMany(x => x.BannerMedias)
                .HasForeignKey(x => x.BannerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.DesktopMediaFile)
                .WithMany()
                .HasForeignKey(x => x.DesktopMediaFileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.MobileMediaFile)
                .WithMany()
                .HasForeignKey(x => x.MobileMediaFileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.AltText).HasMaxLength(255);

            builder.Property(x => x.MediaType).HasConversion<int>();
            builder.Property(x => x.DesktopX)
                .HasPrecision(5, 2);

            builder.Property(x => x.DesktopY)
                .HasPrecision(5, 2);

            builder.Property(x => x.DesktopWidth)
                .HasPrecision(5, 2);

            builder.Property(x => x.MobileX)
                .HasPrecision(5, 2);

            builder.Property(x => x.MobileY)
                .HasPrecision(5, 2);

            builder.Property(x => x.MobileWidth)
                .HasPrecision(5, 2);
            builder.HasIndex(x => new
            {
                x.BannerId,
                x.SortOrder
            });

            // Mỗi Banner chỉ có một ảnh nền
            builder.HasIndex(x => x.BannerId).IsUnique().HasFilter("\"MediaType\" = 1");
        }
    }
}
