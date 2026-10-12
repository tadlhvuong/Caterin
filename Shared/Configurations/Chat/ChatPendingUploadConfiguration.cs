using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;

namespace Shared.Configurations.Chat
{
    public class ChatPendingUploadConfiguration : IEntityTypeConfiguration<ChatPendingUpload>
    {
        public void Configure(EntityTypeBuilder<ChatPendingUpload> builder)
        {
            builder.ToTable("chat_pending_upload");

            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Status, x.ExpiresAt });

            builder.HasIndex(x => x.MediaFileId);

            builder.HasIndex(x => x.GuestSessionId);

            builder.HasIndex(x => x.MessageId);

            builder.Property(x => x.UploadTokenHash).HasMaxLength(64);

            builder.HasOne(x => x.MediaFile)
                .WithMany()
                .HasForeignKey(x => x.MediaFileId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GuestSession)
                .WithMany()
                .HasForeignKey(x => x.GuestSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Message)
                .WithMany()
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.SetNull);


            builder.HasIndex(x => new
            {
                x.UploadTokenHash,
                x.Status,
                x.ExpiresAt
            });
        }

    }
}
