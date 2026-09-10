using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Product
{
    public class ProductTag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(120)]
        public string Slug { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Description { get; set; }

        [MaxLength(180)]
        public string? SeoTitle { get; set; }

        [MaxLength(320)]
        public string? SeoDescription { get; set; }

        public bool NoIndex { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public ICollection<ProductTagMapping> ProductTagMappings { get; set; } = [];
    }
}
