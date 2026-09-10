namespace Shared.Data.Entities.Identity.Log
{
    public class PermissionAuditLog : LogBase
    {
        public long RoleId { get; set; }

        public string AddedPermissions { get; set; }

        public string RemovedPermissions { get; set; }
    }
}
