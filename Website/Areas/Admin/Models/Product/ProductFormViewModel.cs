using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models.Product
{
    public class ProductFormViewModel
    {
        // =========================================================
        // ID
        // =========================================================

        public int? Id { get; set; }


        // =========================================================
        // BASIC INFORMATION
        // =========================================================

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [StringLength(250)]
        public string Name { get; set; } = string.Empty;

        [StringLength(250)]
        public string Slug { get; set; } = string.Empty;

        public string? Description { get; set; }


        // =========================================================
        // CATEGORY
        // =========================================================

        public int CategoryId { get; set; }


        // =========================================================
        // DEFAULT PRICE / STOCK
        //
        // Chỉ sử dụng khi sản phẩm KHÔNG có Variant
        // =========================================================

        [Range(0.01, 999999999)]
        public decimal? Price { get; set; }

        [Range(0, 999999999)]
        public decimal? Stock { get; set; }


        // =========================================================
        // PRODUCT INFORMATION
        // =========================================================

        public string? Unit { get; set; }

        [Range(0, 999999)]
        public decimal? Weight { get; set; }

        public string? Tags { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }


        // =========================================================
        // SEO
        // =========================================================

        [StringLength(200)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }


        // =========================================================
        // PRODUCT IMAGES
        //
        // Dùng để render dữ liệu ảnh hiện tại khi Update.
        // =========================================================

        public List<ProductImageViewModel> ProductImages { get; set; } = [];


        // =========================================================
        // OPTIONS
        //
        // JSON string dùng để hydrate ProductEditor.
        // =========================================================

        public string? Options { get; set; }


        // =========================================================
        // VARIANTS
        //
        // JSON string dùng để hydrate ProductEditor.
        // =========================================================

        public string? Variants { get; set; }


        // =========================================================
        // VARIANT IMAGES
        //
        // Dữ liệu hiện tại để hydrate FilePond khi Update.
        // =========================================================

        public List<ProductVariantImageViewModel> VariantImages { get; set; } = [];


        // =========================================================
        // MODE
        // =========================================================

        public bool IsEdit => Id.HasValue;
    }
}
