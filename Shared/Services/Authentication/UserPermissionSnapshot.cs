namespace Shared.Services.Authentication
{
    public sealed class UserPermissionSnapshot
    {
        public HashSet<int> PermissionIds { get; set; } = new();
        public bool IsRoot { get; set; }
    }
}
