using Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Auth
{
    public enum ExternalLoginStatus
    {
        Authenticated,
        EmailAlreadyExists,
        RegistrationRequired,
        Failed
    }

    public sealed class ExternalLoginResult
    {
        public ExternalLoginStatus Status { get; init; }

        // Chỉ có giá trị khi Authenticated.
        public AuthResponse? AuthResponse { get; init; }

        public string? Message { get; init; }

        public static ExternalLoginResult Authenticated(AuthResponse response)
            => new()
            {
                Status = ExternalLoginStatus.Authenticated,
                AuthResponse = response
            };

        public static ExternalLoginResult EmailAlreadyExists(string message)
            => new()
            {
                Status = ExternalLoginStatus.EmailAlreadyExists,
                Message = message
            };

        public static ExternalLoginResult RegistrationRequired()
            => new()
            {
                Status = ExternalLoginStatus.RegistrationRequired
            };

        public static ExternalLoginResult Failed(string message)
            => new()
            {
                Status = ExternalLoginStatus.Failed,
                Message = message
            };
    }
}
