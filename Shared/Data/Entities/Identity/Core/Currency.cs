using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class Currency
    {
        public int Id { get; set; }

        public string Code { get; set; } = null!;
        // VND

        public string Name { get; set; } = null!;
        // Vietnamese Dong

        public string Symbol { get; set; } = null!;
        // ₫

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
