using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Entities.Identity.Core
{
    public class RolePermission
    {
        public string RoleId { get; set; } = null!;
        public int PermissionId { get; set; }

        public AppRole Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
        public string? AssignedById { get; set; }
        public AppUser? AssignedByUser { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }

}
