namespace Shared.Enums
{
    public enum SecurityActionType
    {
        Login,
        ExternalLogin,
        Logout,
        RefreshToken,
        RefreshTokenReuse,
        Register,
        ForgotPassword,
        ChangePassword,
        ResetPassword,

        ConfirmEmail,
        ConfirmPhone,
        ResendConfirmEmail,

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
