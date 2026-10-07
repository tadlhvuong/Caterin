namespace Shared.DTOs
{
    public class ChatOperationResult
    {
        public bool Succeeded { get; set; }

        public string Message { get; set; } = string.Empty;

        public long ConversationId { get; set; }

        public List<long> MessageIds { get; set; } = new();

        public static ChatOperationResult Success(long conversationId, params long[] messageIds)
        {
            return new ChatOperationResult
            {
                Succeeded = true,
                ConversationId = conversationId,
                MessageIds = messageIds.ToList()
            };
        }

        public static ChatOperationResult Fail(string message)
        {
            return new ChatOperationResult
            {
                Succeeded = false,
                Message = message
            };
        }
    }
}
