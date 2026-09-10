using System.ComponentModel.DataAnnotations;

namespace Shared.Requests.Product
{
    public class ProductRequest
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        [Range(0, 999999999)]
        public decimal? Price { get; set; }

        [Range(0, 999999999)]
        public int? Stock { get; set; }

        public string? Unit { get; set; }

        [Range(0, 999999)]
        public decimal? Weight { get; set; }

        public bool IsFeatured { get; set; }

        [StringLength(200)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        public string? Description { get; set; }

        public List<ProductImageRequest> ProductImages { get; set; } = [];
        public string? Tags { get; set; }

        public string? Options { get; set; }

        public string? Variants { get; set; }

        public List<ProductVariantImageRequest> VariantImages { get; set; } = [];

        public string Action { get; set; } = "draft";  //isActive
    }
}
