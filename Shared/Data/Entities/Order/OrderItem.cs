using Shared.Data.Entities.Inventory;
using Shared.Data.Entities.Product;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Order
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        public int WarehouseId { get; set; }

        // Snapshot product information
        [Required]
        [MaxLength(250)]
        public string ProductName { get; set; } = null!;

        // Snapshot variant information
        [Required]
        [MaxLength(200)]
        public string VariantName { get; set; } = null!;

        // Unit price at the time of purchase
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Price * Quantity
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Total { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation
        public virtual Order Order { get; set; } = null!;
        public virtual ProductVariant ProductVariants { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}
