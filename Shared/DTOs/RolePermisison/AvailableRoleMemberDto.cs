using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.RolePermisison
{
    public sealed class AvailableRoleMemberDto
    {
        public string Id { get; init; } = string.Empty;
        public string? UserName { get; init; }
        public string? Email { get; init; }
        public string? Avatar { get; init; }
        public bool IsActive { get; init; }
        public bool IsLocked { get; init; }
    }
}
