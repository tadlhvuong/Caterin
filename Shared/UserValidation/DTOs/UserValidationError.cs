namespace Shared.UserValidation.DTOs
{
    public enum UserValidationError
    {
        None,

        UserNotFound,

        UserDeleted,

        UserInactive,

        UserLockedOut,

        EmailNotConfirmed,

        PhoneNotConfirmed,

        InvalidSecurityStamp,

        InvalidRefreshToken,

        RefreshTokenNotFound,

        RefreshTokenExpired,

        RefreshTokenRevoked,

        PermissionVersionChanged,
        PermissionVersionInvalid,
        PermissionVersionMissed,

        PrincipalMissed,

        SecurityStampMissed,
        SecurityStampChanged,
    }
}
