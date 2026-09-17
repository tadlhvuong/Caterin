using Shared.Enums.Chat;

namespace Shared.DTOs.Chat
{
    public class ChatMessageDto
    {
        public long Id { get; set; }

        public long ConversationId { get; set; }

        public long InboxId { get; set; }

        public long? ContactId { get; set; }

        public string? SenderId { get; set; }

        public string? SenderName { get; set; }

        public string? SenderAvatar { get; set; }

        public ChatMessageType MessageType { get; set; }

        public ChatMessageContentType ContentType { get; set; }

        public ChatMessageStatus Status { get; set; }

        public ChatSenderType SenderType { get; set; }

        public string? Content { get; set; }

        public bool IsPrivate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
