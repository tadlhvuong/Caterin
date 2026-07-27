using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses
{
    public class AuthResponse
        {
            public bool Success { get; set; }

            public string Message { get; set; }

            public string AccessToken { get; set; }

            public string RefreshToken { get; set; }

            public string UserId { get; set; }

            public string UserName { get; set; }

            public string Email { get; set; }
            public DateTime ExpireAt { get; set; }
            public IReadOnlyList<string> Errors { get; init; } = [];

        public static AuthResponse Fail(string message)
        {
            return new()
            {
                Success = false,
                Message = message
            };
        }

        public static AuthResponse Fail(IEnumerable<string> errors)
            => new()
            {
                Success = false,
                Errors = errors.ToList()
            };
    }

}
