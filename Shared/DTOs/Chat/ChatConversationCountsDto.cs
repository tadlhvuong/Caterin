using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class ChatConversationCountsDto
    {
        public int All { get; set; }
        public int Open { get; set; }
        public int Pending { get; set; }
        public int Resolved { get; set; }
        public int Unread { get; set; }
    }
}
