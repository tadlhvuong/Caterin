using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Configurations.Chat
{
    public sealed class ChatAttachmentConfiguration
    : IEntityTypeConfiguration<ChatAttachment>
    {
        public void Configure(EntityTypeBuilder<ChatAttachment> builder)
        {
            builder.ToTable("chat_attachments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.MessageId).IsRequired();

            builder.Property(x => x.MediaFileId).IsRequired(false);

            builder.Property(x => x.FileName).IsRequired().HasMaxLength(500);

            builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);

            builder.Property(x => x.FileSize).IsRequired();

            builder.HasOne(x => x.Message).WithMany(x => x.Attachments)
                .HasForeignKey(x => x.MessageId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.MediaFile).WithMany()
                .HasForeignKey(x => x.MediaFileId).OnDelete(DeleteBehavior.SetNull);
            // =========================
            // Indexes
            // =========================

            builder.HasIndex(x => x.MessageId);

            builder.HasIndex(x => x.MediaFileId);
        }
    }
}
