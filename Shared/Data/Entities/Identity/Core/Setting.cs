using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class Setting
    {
        public long Id { get; set; }

        public string Key { get; set; } = null!;

        public string Value { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsSystem { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
