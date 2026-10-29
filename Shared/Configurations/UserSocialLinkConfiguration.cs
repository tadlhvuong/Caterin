using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Configurations
{
    public class UserSocialLinkConfiguration : IEntityTypeConfiguration<UserSocialLink>
    {
        public void Configure(EntityTypeBuilder<UserSocialLink> builder)
        {
            builder.ToTable("UserSocialLinks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.Property(x => x.Provider).IsRequired().HasMaxLength(30);

            builder.Property(x => x.Url).IsRequired().HasMaxLength(500);

            builder.HasOne(x => x.User).WithMany(x => x.SocialLinks)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => new { x.UserId, x.Provider }).IsUnique();
        }
    }
}