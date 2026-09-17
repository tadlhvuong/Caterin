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
    public sealed class ChatContactInboxConfiguration : IEntityTypeConfiguration<ChatContactInbox>
    {
        public void Configure(EntityTypeBuilder<ChatContactInbox> builder)
        {
            builder.ToTable("chat_contact_inboxes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.ContactId).IsRequired();

            builder.Property(x => x.InboxId).IsRequired();

            builder.Property(x => x.SourceId).HasMaxLength(500).IsRequired(false);

            builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone").IsRequired();
            
            builder.HasOne(x => x.Contact).WithMany(x => x.Inboxes)
                .HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Inbox).WithMany(x => x.Contacts)
                .HasForeignKey(x => x.InboxId).OnDelete(DeleteBehavior.Cascade);

            // =========================
            // Indexes
            // =========================

            builder.HasIndex(x => new
            {
                x.ContactId,
                x.InboxId
            })
            .IsUnique();

            builder.HasIndex(x => x.InboxId);
        }
    }
}
