using Shared.Enums;

namespace Shared.Responses.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }

        public string SKU { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public decimal? Price { get; set; }

        public decimal? Stock { get; set; }

        public string? Unit { get; set; }

        public decimal? Weight { get; set; }

        public ICollection<string> Tags { get; set; } = new List<string>();

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public string? MetaTitle { get; set; }

        public string? MetaDescription { get; set; }

        public string? Description { get; set; }

        public ProductStatus Status { get; set; }

        public List<ProductImageResponse> Images { get; set; } = [];

        public List<ProductOptionResponse> Options { get; set; } = [];

        public List<ProductVariantResponse> Variants { get; set; } = [];

        public List<ProductVariantImageResponse> VariantImages { get; set; } = [];
    }
}
