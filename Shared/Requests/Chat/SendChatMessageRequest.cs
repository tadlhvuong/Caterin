using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Chat
{
    public class SendChatMessageRequest
    {
        public long ConversationId { get; set; }

        [Required]
        [MaxLength(150000)]
        public string Content { get; set; } = null!;
    }
}
