using Microsoft.AspNetCore.Http;
using Shared.Data.Entities.Product;
using Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product
{
    public class CreateProductRequest
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

        // Chỉ sử dụng khi KHÔNG có Variant
        [Range(0, 999999999)]
        public decimal? Price { get; set; }

        // Chỉ sử dụng khi KHÔNG có Variant
        [Range(0, 999999999)]
        public int? Stock { get; set; }

        public string? Unit { get; set; }

        [Range(0, 999999)]
        public decimal? Weight { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        [StringLength(200)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        public string? Description { get; set; }

        public List<CreateProductImageRequest> ProductImages { get; set; } = [];
        public string? Tags { get; set; }

        public string? Options { get; set; }

        //public List<CreateProductVariantRequest>? Variants { get; set; }
        public string? Variants { get; set; }

        //public Dictionary<string, IFormFile>? VariantImages { get; set; }
        public List<CreateProductVariantImageRequest> VariantImages { get; set; } = [];

        public string Action { get; set; } = "draft";
    }

    public class CreateProductOptionRequest
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = [];
    }
        public class CreateProductVariantRequest
    {
        public int? Id { get; set; }
        public Dictionary<string, string> Options { get; set; } = [];

            public decimal Price { get; set; }

            public int Stock { get; set; }

            public string SKU { get; set; }
        }
    public class CreateProductVariantImageRequest
    {
        public string Key { get; set; } = string.Empty;
        public long? Id { get; set; }

        public IFormFile? File { get; set; } = null!;
    }
    public class CreateProductImageRequest
    {
        public int? Id { get; set; }
        public IFormFile? File { get; set; } = default!;

        public int DisplayOrder { get; set; }

        public bool IsPrimary { get; set; }
    }
}
