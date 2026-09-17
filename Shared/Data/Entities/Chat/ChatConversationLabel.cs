namespace Shared.Data.Entities.Chat
{
    public class ChatConversationLabel
    {
        public long ConversationId { get; set; }

        public int LabelId { get; set; }

        public ChatConversation Conversation { get; set; } = null!;

        public ChatLabel Label { get; set; } = null!;
    }
}
