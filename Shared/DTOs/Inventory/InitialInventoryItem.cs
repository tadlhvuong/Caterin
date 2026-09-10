using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Inventory
{
    public sealed class InitialInventoryItem
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
