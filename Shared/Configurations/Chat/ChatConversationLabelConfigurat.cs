using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Data.Entities.Chat;

namespace Shared.Configurations.Chat
{
    public sealed class ChatConversationLabelConfiguration : IEntityTypeConfiguration<ChatConversationLabel>
    {
        public void Configure(EntityTypeBuilder<ChatConversationLabel> builder)
        {
            builder.ToTable("chat_conversation_labels");

            builder.HasKey(x => new
            {
                x.ConversationId,
                x.LabelId
            });

            builder.Property(x => x.ConversationId).IsRequired();

            builder.Property(x => x.LabelId).IsRequired();

            builder.HasOne(x => x.Conversation).WithMany(x => x.Labels)
                .HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Label).WithMany(x => x.Conversations)
                .HasForeignKey(x => x.LabelId).OnDelete(DeleteBehavior.Cascade);

            // Indexes

            builder.HasIndex(x => x.LabelId);
        }
    }
}
