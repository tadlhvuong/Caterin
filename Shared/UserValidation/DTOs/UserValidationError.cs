using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
