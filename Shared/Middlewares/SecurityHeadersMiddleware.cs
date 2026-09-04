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
                //X-Content-Type-Options: nosniff
                //Referrer-Policy: strict-origin-when-cross-origin
                //Permissions-Policy: geolocation=(), microphone=(), camera=()
                //X-Frame-Options: DENY
                //Cross-Origin-Opener-Policy: same-origin
                //Cross-Origin-Resource-Policy: same-origin

                context.Response.Headers["Content-Security-Policy"] =
                   "default-src 'self'; " +
                   "base-uri 'self'; " +
                   "script-src 'self' 'unsafe-inline' https://unpkg.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://maps.googleapis.com https://maps.gstatic.com; " +
                   "style-src 'self' 'unsafe-inline'  https://unpkg.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
                    "worker-src 'self' blob:;" +
                    "img-src 'self'  blob: https://*.googleapis.com https://*.gstatic.com data: https:; " +
                   "font-src 'self' https://fonts.gstatic.com https:; " +
                    "connect-src 'self' https://accounts.google.com https://oauth2.googleapis.com wss://localhost:44382 http://localhost:56754 ws://localhost:56754 https://unpkg.com https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://cdn.jsdelivr.net;" +
                   "object-src 'none';" +
                   "frame-ancestors 'none'; " +
                    "form-action 'self' https://accounts.google.com https://www.facebook.com;" +
                   "upgrade-insecure-requests;" +
                   "block-all-mixed-content;";

                context.Response.Headers["Permissions-Policy"] = 
                    "geolocation=(self), " +
                    "camera=(), " +
                    "microphone=(), " +
                    "payment=(), " +
                    "usb=(self), " +
                    "serial=(self), " +
                    "bluetooth=(), " +
                    "hid=(self), " +
                    "fullscreen=(self)";
                await next();
            });
        }
    }
}
