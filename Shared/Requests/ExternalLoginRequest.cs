using System.Security.Claims;

namespace Shared.Requests
{
    public sealed class ExternalLoginRequest
    {
        public string Provider { get; set; } = default!;

        public string ProviderKey { get; set; } = default!;

        public string? Email { get; set; }

        public string? Name { get; set; }

        public string? AvatarUrl { get; set; }

        public IEnumerable<Claim> Claims { get; set; } = Enumerable.Empty<Claim>();

        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public bool RememberMe { get; set; } = false;
    }
}
