using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Website.Areas.Admin.Models.Product
{
    public sealed class CreateProductViewModel
    {
        #region Product Information

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? SKU { get; set; }

        [MaxLength(100)]
        public string? Barcode { get; set; }

        public string? Description { get; set; }

        #endregion

        #region Pricing

        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? DiscountedPrice { get; set; }

        public bool ChargeTax { get; set; } = true;

        #endregion

        #region Inventory

        public bool InStock { get; set; }

        public int StockQuantity { get; set; }

        #endregion

        #region Organize

        public Guid? VendorId { get; set; }

        public Guid? CategoryId { get; set; }

        public Guid? CollectionId { get; set; }

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public List<string> Tags { get; set; } = [];

        #endregion

        #region Images

        /// <summary>
        /// Upload từ máy
        /// </summary>
        public List<IFormFile> Images { get; set; } = [];

        /// <summary>
        /// Add media from URL
        /// </summary>
        public List<string> ImageUrls { get; set; } = [];

        #endregion

        #region Variants

        public List<ProductOptionViewModel> Options { get; set; } = [];

        #endregion
    }
}
