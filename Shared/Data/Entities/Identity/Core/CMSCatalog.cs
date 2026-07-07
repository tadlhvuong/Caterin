using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity
{
    using Shared.Enums;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class CMSCatalog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = default!; // product.create

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = default!; // Create Product

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public CatalogType Type { get; set; }

        public int? ParentId { get; set; }

        [ForeignKey(nameof(ParentId))]
        public CMSCatalog? Parent { get; set; }

        public ICollection<CMSCatalog> Children { get; set; } = [];

        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }
        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
