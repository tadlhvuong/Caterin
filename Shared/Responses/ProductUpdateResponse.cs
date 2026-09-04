using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class ProductUpdateResponse
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

        public List<CreateProductOptionResponse> Options { get; set; } = [];

        public List<ProductVariantResponse> Variants { get; set; } = [];

        public List<ProductVariantImageResponse> VariantImages { get; set; } = [];
    }
    public class ProductImageResponse
    {
        public int Id { get; set; }

        public string Url { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }
    }
    public class ProductVariantResponse
    {
        public int Id { get; set; }

        public Dictionary<string, string> Options { get; set; } = [];

        public string SKU { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Stock { get; set; }

        public bool IsDefault { get; set; }

        public bool IsActive { get; set; }
    }
    public class ProductVariantImageResponse
    {
        public string Key { get; set; } = string.Empty;

        public int Id { get; set; }

        public string Url { get; set; } = string.Empty;
    }
    public class CreateProductOptionResponse
    {
        public string Name { get; set; } = string.Empty;

        public List<string> Values { get; set; } = [];
    }
}
