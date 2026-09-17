namespace Shared.Constants.Core
{
    public static class ChatHubGroups
    {
        public static string Conversation(long conversationId)
            => $"chat:conversation:{conversationId}";

        public static string Inbox(long inboxId)
            => $"chat:inbox:{inboxId}";

        public static string Contact(long contactId)
            => $"chat:contact:{contactId}";
    }
}
