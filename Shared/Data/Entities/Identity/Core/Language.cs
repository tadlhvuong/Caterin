using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class Language
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;
        // vi-VN

        public string Name { get; set; } = null!;
        // Vietnamese

        public string NativeName { get; set; } = null!;
        // Tiếng Việt

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
