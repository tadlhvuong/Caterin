using Shared.Data.Entities.Identity;
using Shared.Enums.Chat;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatMessage
    {
        public long Id { get; set; }

        public long ConversationId { get; set; }

        public long InboxId { get; set; }

        public long? ContactId { get; set; }

        public string? UserId { get; set; }

        public ChatMessageType MessageType { get; set; }

        public ChatMessageContentType ContentType { get; set; }

        public ChatMessageStatus Status { get; set; }

        public ChatSenderType SenderType { get; set; }

        [MaxLength(150000)]
        public string? Content { get; set; }

        public bool IsPrivate { get; set; }

        public string? MetadataJson { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ChatConversation Conversation { get; set; } = null!;

        public ChatInbox Inbox { get; set; } = null!;

        public ChatContact? Contact { get; set; }

        public AppUser? User { get; set; }

        public ICollection<ChatAttachment> Attachments { get; set; } = new List<ChatAttachment>();
    }
}
