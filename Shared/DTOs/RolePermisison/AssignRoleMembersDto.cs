using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{
    public class AssignRoleMembersDto
    {
        public string RoleId { get; set; } = null!;
        public List<string> UserIds { get; set; } = [];
    }
}
