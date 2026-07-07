using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class PermissionLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ChangeBy { get; set; } = null!;

        [Required]
        public string RoleId { get; set; } = null!;

        [ForeignKey(nameof(RoleId))]
        public AppRole? Role { get; set; }

        [Required]
        [MaxLength(2000)]
        public string PermissionOld { get; set; } = null!;

        [Required]
        [MaxLength(2000)]
        public string PermissionNew { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
