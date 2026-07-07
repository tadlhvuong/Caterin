using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Authentication
{
    public sealed class UserPermissionSnapshot
    {
        public HashSet<int> PermissionIds { get; set; } = new();
        public bool IsRoot { get; set; }
    }
}
