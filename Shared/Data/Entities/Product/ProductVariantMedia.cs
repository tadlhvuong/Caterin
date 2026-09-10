using Shared.Data.Entities.Media;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Product
{
    public class ProductVariantMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [Required]
        public int AttributeValueId { get; set; }
        [Required]
        public long MediaFileId { get; set; }
        public int DisplayOrder { get; set; }

        public ProductVariant ProductVariant { get; set; } = null!;
        public AttributeValue AttributeValue { get; set; } = null!;
        public MediaFile MediaFile { get; set; } = null!;
    }
}
