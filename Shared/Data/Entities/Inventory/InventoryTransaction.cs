using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Inventory
{
    public class InventoryTransaction
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int ProductVariantId { get; set; }
        public string Type { get; set; }
        public int Quantity { get; set; }
        public int ReferenceId { get; set; }
        public string Note { get; set; }
        public int Createdby {  get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
