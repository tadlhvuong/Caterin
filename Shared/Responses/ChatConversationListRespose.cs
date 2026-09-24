using Shared.DTOs.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class ChatConversationListResponse
    {
        public List<ChatConversationListItemDto> Items { get; set; } = [];

        public bool HasMore { get; set; }

        public DateTime? OldestLastMessageAt { get; set; }

        public long? OldestId { get; set; }
    }
}
