using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Identity.Core
{

    public class Permission
    {
        [Key]
        public int Id { get; set; }
        public int ModuleId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = false;
        public bool IsSystem { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public CMSModule Module { get; set; } = null!;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public virtual ICollection<RoutePermission> RoutePermissions { get; set; }  = new HashSet<RoutePermission>();

    }

}
