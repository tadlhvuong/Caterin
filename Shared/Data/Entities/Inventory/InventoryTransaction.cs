using Shared.Data.Entities.Product;
using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Inventory
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        [Required]
        public int WarehouseId { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        public InventoryTransactionType Type { get; set; }

        [Required]
        public int Quantity { get; set; }

        public int? ReferenceId { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        public int? CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public ProductVariant ProductVariant { get; set; } = null!;
    }
}
