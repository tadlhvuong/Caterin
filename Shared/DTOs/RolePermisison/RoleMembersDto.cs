using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{
    public sealed class RoleMembersDto
    {
        public int Total { get; init; }
        public IReadOnlyList<RoleMemberDto> Members { get; init; }
            = Array.Empty<RoleMemberDto>();
    }
}
