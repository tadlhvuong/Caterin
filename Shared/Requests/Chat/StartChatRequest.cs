using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Chat
{
    public class StartChatRequest
    {
        public long InboxId { get; set; }

        [MaxLength(150)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? GuestToken { get; set; }
    }
}
