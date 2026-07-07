using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class RoutePermission
    {
        [Key]
        public long Id { get; set; }

        public string Route { get; set; } = null!;

        public string HttpMethod { get; set; } = null!;

        public string? Controller { get; set; }

        public string? Action { get; set; }
        public string? PermissionCode { get; set; }

        public int PermissionId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(PermissionId))]
        public virtual Permission Permission { get; set; } = null!;
    }
}
