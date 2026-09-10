using System.ComponentModel.DataAnnotations;
using Shared.Enums;

namespace Shared.Data.Entities.Product
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(250)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(250)]
        public string Slug { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Sku { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ShortDescription { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Giá mặc định hoặc giá bắt đầu.
        /// Giá thực tế của variant nằm ở ProductVariant.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? Price { get; set; }
        [Range(0, 999999999)]
        public decimal? Stock { get; set; }
        /// <summary>
        /// Khối lượng sản phẩm, tính theo WeightUnit.
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal? Weight { get; set; }

        [MaxLength(20)]
        public WeightUnit? WeightUnit { get; set; }
        public bool IsFeatured { get; set; }
        public int DisplayOrder { get; set; }

        [MaxLength(250)]
        public string? SeoTitle { get; set; }

        [MaxLength(500)]
        public string? SeoDescription { get; set; }

        [MaxLength(500)]
        public string? CanonicalUrl { get; set; }

        public bool NoIndex { get; set; }

        public ProductStatus Status { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public DateTime? PublishedAt { get; set; }
        public bool IsDeleted { get; set; }
        
        [Required]
        public int CategoryId { get; set; }

        public ProductCategory Category { get; set; } = null!;

        public ICollection<ProductTagMapping> ProductTagMappings { get; set; } = [];

        public virtual ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        public virtual ICollection<ProductMedia> ProductMedias { get; set; } = new List<ProductMedia>();
    }
}
