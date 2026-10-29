using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{

    public sealed class RolePermissionSelectionDto
    {
        public int ModuleId { get; set; }
        public string Action { get; set; } = null!;
    }
}
