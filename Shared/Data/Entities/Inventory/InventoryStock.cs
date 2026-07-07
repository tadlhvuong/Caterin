using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Inventory
{
    public class InventoryStock
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int MinStock { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
