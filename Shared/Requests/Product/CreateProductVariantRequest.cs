using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product
{
    public class CreateProductVariantRequestOld
    {
        public string VariantKey { get; set; } = null!;

        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SKU { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Barcode { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CompareAtPrice { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; }

        public ICollection<CreateVariantAttributeRequest> Attributes { get; set; } = [];
    }
}
