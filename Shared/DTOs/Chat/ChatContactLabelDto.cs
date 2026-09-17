using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class ChatContactLabelDto
    {
        public long Id { get; set; }

        public string Name { get; set; } = null!;

        public string? Color { get; set; }
    }   
}
