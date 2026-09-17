using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class ChatConversationContactDto
    {
        public long ConversationId { get; set; }

        public long ContactId { get; set; }

        public string? Name { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? AvatarUrl { get; set; }

        public string? CustomAttributesJson { get; set; }

        public DateTime? CreatedAt { get; set; }

        public List<ChatContactLabelDto> Labels { get; set; } = new();

        public List<ChatRecentOrderDto> RecentOrders { get; set; } = new();
    }
}
