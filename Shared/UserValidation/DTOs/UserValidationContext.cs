using Microsoft.AspNetCore.Http;
using Shared.Data.Entities.Identity;
using Shared.Services.Authentication;
using System.Security.Claims;

namespace Shared.UserValidation.DTOs
{
    public class UserValidationContext
    {
        public UserValidationScenario Scenario { get; init; }

        public string? UserId { get; init; }
        public AppUser User { get; init; } = default!;

        public UserPermissionSnapshot? PermissionSnapshot { get; set; }

        public ClaimsPrincipal? Principal { get; init; }

        public HttpContext? HttpContext { get; init; }

        public string? RefreshToken { get; init; }
        public RefreshToken? RefreshTokenEntity { get; init; }
        public IReadOnlyList<string>? Permissions { get; set; }
    }
}
