using Shared.Data.Entities.Product;
using Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Media
{
    public class MediaFile
    {
        [Key]
        public long Id { get; set; }

        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string StoragePath { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string ContentType { get; set; } = null!;

        public long Size { get; set; }

        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        public DateTime CreatedAt { get; set; }

        public virtual ICollection<ProductMedia> ProductMedias { get; set; }
            = new List<ProductMedia>();

        public virtual ICollection<ProductVariantMedia> ProductVariantMedias { get; set; }
            = new List<ProductVariantMedia>();
    }
}
