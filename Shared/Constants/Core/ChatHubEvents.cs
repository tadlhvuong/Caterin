namespace Shared.Constants.Core
{
    public static class ChatHubEvents
    {
        public const string MessageReceived = "chat.message.received";

        public const string MessageDelivered = "chat.message.delivered";

        public const string MessageRead = "chat.message.read";

        public const string MessageStatusUpdated = "chat.message.status.updated";

        public const string ConversationRead = "chat.conversation.read";

        public const string ConversationCreated = "chat.conversation.created";

        public const string ConversationUpdated = "chat.conversation.updated";

        public const string ConversationStatusUpdated = "chat.conversation.status.updated";

        public const string ConversationAssigned = "chat.conversation.assigned";

        public const string ConversationClosed = "chat.conversation.closed";

        public const string Typing = "chat.typing";

        public const string AdminOnline = "chat.admin.online";

        public const string AdminOffline = "chat.admin.offline";

        public const string AdminPresenceUpdated = "chat.admin.presence.updated";

        public const string AdminConvensationPresenceUpdated = "chat.admin.presence.updated";

    }
}
