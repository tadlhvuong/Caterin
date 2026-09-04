using Shared.Data.Entities.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // =========================
        // Navigation
        // =========================

        public MediaFile MediaFile { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
    }
}
