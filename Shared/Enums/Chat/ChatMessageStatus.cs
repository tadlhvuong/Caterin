using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Enums.Chat
{
    public enum ChatMessageStatus
    {
        Pending = 1,
        Sent = 2,
        Delivered = 3,
        Read = 4,
        Failed = 5
    }
}
