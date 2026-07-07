using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Middlewares
{
    public static class SecurityHeadersMiddleware
    {
        //secure CSF
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.Use(async (context, next) =>
            {
                context.Response.Headers["Content-Security-Policy"] =
                   "default-src 'self'; " +
                   "script-src 'self' 'unsafe-inline' https://unpkg.com https://cdn.jsdelivr.net; " +
                   "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
                   "img-src 'self' data: https:; " +
                   "font-src 'self' https://fonts.gstatic.com https:; " +
                   "connect-src 'self'; " +
                   "frame-ancestors 'none'; " +
                   "base-uri 'self'; " +
                   "form-action 'self';";
                await next();
            });
        }
    }
}
