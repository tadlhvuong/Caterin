using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Chat
{
    public class GuestContactResolution
    {
        public ChatContact Contact { get; init; } = null!;

        public string GuestToken { get; init; } = null!;
    }
}
