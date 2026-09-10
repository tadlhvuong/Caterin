using Shared.Data.Entities.Product;

namespace Shared.Data.Entities.Inventory
{
    public class InventoryStock
    {
        public int Id { get; set; }
        public int WarehouseId { get; set; }
        public int ProductVariantId { get; set; }
        public int AvailableQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int MinStock { get; set; }
        public DateTime CreateAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public Warehouse Warehouse { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;
    }
}
