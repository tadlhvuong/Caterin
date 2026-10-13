namespace Shared.DTOs.Chat
{
    public class ChatConversationHeaderDto
    {
        public long ConversationId { get; set; }
        public long ContactId { get; set; }
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
