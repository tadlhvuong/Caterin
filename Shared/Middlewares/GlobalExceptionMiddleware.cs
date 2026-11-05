using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Shared.Middlewares
{
    public sealed class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(
            RequestDelegate next,
            ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Access denied. Path: {Path}",
                    context.Request.Path);

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();

                var isAjax =
                    context.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                var acceptsJson =
                    context.Request.Headers.Accept.Any(x =>
                        x?.Contains(
                            "application/json",
                            StringComparison.OrdinalIgnoreCase) == true);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;

                if (isAjax || acceptsJson)
                {
                    await context.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Bạn không có quyền thực hiện thao tác này."
                    });
                }
                else
                {
                    context.Response.Redirect("/error/403");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception. Path: {Path}",
                    context.Request.Path);

                if (context.Response.HasStarted)
                    throw;

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
                });
            }
        }
    }
}
