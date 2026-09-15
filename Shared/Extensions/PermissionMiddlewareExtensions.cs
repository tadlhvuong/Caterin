using Microsoft.AspNetCore.Builder;
using Shared.Middlewares;

namespace Shared.Extensions
{
    public static class PermissionMiddlewareExtensions
    {
        public static IApplicationBuilder UsePermissionMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PermissionMiddleware>();
        }
    }
}
