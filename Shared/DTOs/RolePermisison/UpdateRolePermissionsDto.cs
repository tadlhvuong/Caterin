using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{
    public sealed class UpdateRolePermissionsDto
    {
        public string RoleId { get; set; } = null!;
        public List<RolePermissionSelectionDto> Selections { get; set; } = [];
    }
}
