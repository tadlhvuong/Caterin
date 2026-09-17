using Shared.Enums.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class ChatConversationDetailDto
    {
        public long Id { get; set; }
        public long InboxId { get; set; }
        public long ContactId { get; set; }

        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactAvatarUrl { get; set; }

        public string? AssignedUserId { get; set; }
        public string? AssignedUserName { get; set; }

        public long? TeamId { get; set; }
        public string? TeamName { get; set; }

        public string? Subject { get; set; }

        public ChatConversationStatus Status { get; set; }
        public ChatConversationPriority Priority { get; set; }

        public DateTime? LastMessageAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<ChatMessageDto> Messages { get; set; } = new();
    }
}
