using Shared.Enums.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class CustomerSendMessageResult
    {
        public ChatMessageDto Message { get; set; } = null!;

        public long ContactId { get; set; }

        public long ConversationId { get; set; }

        public ChatConversationStatus Status { get; set; }

        // Chỉ có giá trị khi server vừa tạo GuestSession mới.
        public string? GuestToken { get; set; }

        public bool IsNewContact { get; set; }

        public bool IsNewConversation { get; set; }
    }
}
