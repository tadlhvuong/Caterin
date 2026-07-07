using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class CMSModule
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string Code { get; set; }

        [MaxLength(100)]
        public string? Icon { get; set; }

        public bool IsSystem { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public ICollection<Permission> Permissions { get; set; } = [];
    }
}
