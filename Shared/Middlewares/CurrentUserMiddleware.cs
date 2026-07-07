using Microsoft.AspNetCore.Http;
using Shared.Interfaces.IdentityServices;

namespace Shared.Middlewares
{
    public class CurrentUserMiddleware
    {
        private readonly RequestDelegate _next;

        public CurrentUserMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ICurrentUserService currentUser)
        {
            // Force resolve CurrentUser early (warm up)
            if (context.User.Identity?.IsAuthenticated == true)
            {
                _ = currentUser.UserId;
            }

            await _next(context);
        }
    }
}
