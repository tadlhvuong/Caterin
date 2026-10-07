using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public sealed class StartChatResponse
    {
        public long? ContactId { get; set; }

        public long InboxId { get; set; }

        public long? ConversationId { get; set; }

        public string? ContactName { get; set; }

        public string? ContactAvatar { get; set; }
    }
}
