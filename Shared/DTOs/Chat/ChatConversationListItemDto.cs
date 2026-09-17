using Shared.Enums.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class ChatConversationListItemDto
    {
        public long Id { get; set; }

        public long InboxId { get; set; }

        public long ContactId { get; set; }

        public string? ContactName { get; set; }

        public string? ContactAvatarUrl { get; set; }

        public ChatConversationStatus Status { get; set; }

        public ChatConversationPriority Priority { get; set; }

        public string? Subject { get; set; }

        public string? LastMessage { get; set; }

        public DateTime LastMessageAt { get; set; }

        public string? AssignedUserId { get; set; }

        public int? TeamId { get; set; }

        //public int UnreadCount { get; set; }

        public bool IsAssignedToCurrentUser { get; set; }

        public bool IsUnassigned => string.IsNullOrEmpty(AssignedUserId);

        public List<ChatContactLabelDto> Labels { get; set; } = new();
    }
}
