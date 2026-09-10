using Shared.Data.Entities.Media;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Product
{
    public class ProductMedia
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public long MediaFileId { get; set; }

        public bool IsPrimary { get; set; }

        public int DisplayOrder { get; set; }
        public MediaFile MediaFile { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
