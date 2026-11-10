using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Constants.Core
{
    public static class DataProtectionPurposes
    {
        public const string EmailActionToken =
        "Caterin.EmailAction.Token.v1";

        public const string RefreshTokenRotation =
            "Caterin.RefreshToken.Rotation.v1";

        public const string DeviceIdentity =
            "Caterin.Security.DeviceIdentity.v1";
    }
}
