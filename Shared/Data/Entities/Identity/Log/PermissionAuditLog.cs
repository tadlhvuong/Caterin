using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Log
{
    public class PermissionAuditLog : LogBase
    {
        public long RoleId { get; set; }

        public string AddedPermissions { get; set; }

        public string RemovedPermissions { get; set; }
    }
}
