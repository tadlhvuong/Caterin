using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Media;

namespace Shared.Configurations.Media
{
    public class MediaAlbumConfiguration : IEntityTypeConfiguration<MediaAlbum>
    {
        public void Configure(EntityTypeBuilder<MediaAlbum> builder)
        {
            builder.ToTable("MediaAlbums");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.Name).IsRequired().HasMaxLength(250);

            builder.Property(x => x.Description).HasMaxLength(500);

            builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");

            builder.Property(x => x.UpdatedAt);

            builder.HasIndex(x => x.Name);
        }
    }
}