using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Auth
{
    public sealed class ExternalRegistrationRequest
    {
        public string Provider { get; set; } = default!;
        public string ProviderKey { get; set; } = default!;

        public string Email { get; set; } = default!;
        public string UserName { get; set; } = default!;

        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }

        public bool AcceptTerms { get; set; }
        public bool AcceptPrivacy { get; set; }
    }
}
