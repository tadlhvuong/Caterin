using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatContactInbox
    {
        public long Id { get; set; }

        public long ContactId { get; set; }

        public long InboxId { get; set; }

        [MaxLength(500)]
        public string? SourceId { get; set; }

        public DateTime CreatedAt { get; set; }

        public ChatContact Contact { get; set; } = null!;
        public ChatInbox Inbox { get; set; } = null!;
    }
}
