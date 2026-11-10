using Shared.Data.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Auth
{
    public enum ExternalLoginResolutionStatus
    {
        LinkedUser,
        EmailAlreadyExists,
        RegistrationRequired
    }

    public sealed class ExternalLoginResolution
    {
        public ExternalLoginResolutionStatus Status { get; init; }

        public AppUser? User { get; init; }

        public static ExternalLoginResolution Linked(AppUser user)
            => new()
            {
                Status = ExternalLoginResolutionStatus.LinkedUser,
                User = user
            };

        public static ExternalLoginResolution ExistingEmail()
            => new()
            {
                Status = ExternalLoginResolutionStatus.EmailAlreadyExists
            };

        public static ExternalLoginResolution RegistrationRequired()
            => new()
            {
                Status = ExternalLoginResolutionStatus.RegistrationRequired
            };
    }
}
