using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{
    public class RoleCardDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Level { get; set; }
        public bool IsSystem { get; set; }
        public int MemberCount { get; set; }
        public int PermissionCount { get; set; }
    }
}
