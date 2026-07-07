namespace Shared.Enums
{
    public enum SecurityActionType
    {
        Login,
        Logout,
        RefreshToken,
        Register,
        ForgotPassword,
        ChangePassword,
        ResetPassword,

        JwtIssued,
        JwtRevoked,

        AdminAccessDenied,
        ForbiddenAccess,

        RoleReplaced,
        RoleAssigned,
        RoleRemoved,
        RoleDenied,
        RoleBulkUpdated,

        PermissionChanged,
        PermissionAssigned,
        PermissionDenied,

        Lock,
        UnLock,
        Enable,
        Disable,

    }
}
