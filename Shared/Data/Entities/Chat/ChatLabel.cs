using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Chat
{
    public class ChatLabel
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? Color { get; set; }

        public bool IsActive { get; set; }
        public ICollection<ChatConversationLabel> Conversations { get; set; } = new List<ChatConversationLabel>();
    }
}
