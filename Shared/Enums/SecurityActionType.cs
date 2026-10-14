namespace Shared.Enums
{
    public enum SecurityActionType
    {
        Login,
        ExternalLogin,
        Logout,

        RefreshToken,
        RefreshTokenReuse,
        JwtIssued,
        JwtRevoked,

        RefreshTokenCreated,
        RefreshTokenRevoked,
        RefreshTokenExpired,

        Register,
        ForgotPassword,
        ChangePassword,
        ResetPassword,

        ConfirmEmail,
        ConfirmPhone,
        ResendConfirmEmail,

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
