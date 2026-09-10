using Shared.Enums;

namespace Shared.DTOs.Product
{
    public class ProductListResult
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        public string? ImageUrl { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public int CategoryId { get; set; }

        public string Sku { get; set; } = string.Empty;

        public decimal? MinPrice { get; set; }

        // Giá cao nhất
        public decimal? MaxPrice { get; set; }
        /// <summary>
        /// Tổng tồn kho khả dụng.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Có hàng hay không.
        /// </summary>
        public bool InStock { get; set; }

        public ProductStatus Status { get; set; }

        public bool IsFeatured { get; set; }

        public int DisplayOrder { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
