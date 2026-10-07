using Shared.Enums.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class SendCustomerMessageResult
    {
        public long ContactId { get; set; }

        public long ConversationId { get; set; }

        public ChatConversationStatus Status { get; set; }

        public string? GuestToken { get; set; }

        public ChatMessageDto Message { get; set; } = null!;
    }
}
