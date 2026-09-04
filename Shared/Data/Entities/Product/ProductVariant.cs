
using Shared.Data.Entities.Inventory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Product
{

    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }
        // =========================
        // Variant information
        // =========================

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;


        [MaxLength(50)]
        public string? Barcode { get; set; }
        // =========================
        // Pricing
        // =========================

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        /// <summary>
        /// Giá niêm yết / giá trước khi giảm.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? CompareAtPrice { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }

        // =========================
        // Audit
        // =========================

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        // =========================
        // Navigation
        // =========================

        public Product Product { get; set; } = null!;
        public ICollection<VariantAttribute> VariantAttributes { get; set; } = [];
        public ICollection<ProductVariantMedia> VariantMedias { get; set; } = [];
        public ICollection<InventoryStock> InventoryStocks { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    }
}
