using Microsoft.AspNetCore.Http;
using Shared.Interfaces.IdentityServices;
using Shared.Constants.Permission;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Shared.Services
{

    public sealed class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

        public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

        public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

        public string? UserName => User?.FindFirstValue(ClaimConstants.UserName);

        public string? Email => User?.FindFirstValue(ClaimTypes.Email);

        public IReadOnlyList<string> Roles => User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? new List<string>();
        
        public string? SecurityStamp => User?.FindFirstValue(ClaimConstants.SecurityStamp);

        public int PermissionVersion => int.TryParse(User?.FindFirstValue(ClaimConstants.PermissionVersion),
           out var version) ? version : 0;
    }
}
