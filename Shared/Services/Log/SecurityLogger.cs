using Microsoft.AspNetCore.Http;
using Shared.Data.Entities.Identity.Log;
using Shared.Enums;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Log
{
    public sealed class SecurityLogger
    : ISecurityLogger
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogPipeline _pipeline;

        public SecurityLogger(
            ICurrentUserService currentUser,
            IHttpContextAccessor httpContext,
            ILogPipeline pipeline)
        {
            _currentUser = currentUser;
            _httpContext = httpContext;
            _pipeline = pipeline;
        }

        public async Task LogAsync(
            SecurityActionType action,
            bool success,
            string message)
        {
            var log = new SecurityLog
            {
                UserId = _currentUser.UserId,
                UserName = _currentUser.UserName,

                ActionType = action,

                IsSuccess = success,

                Message = message,

                IpAddress = _httpContext.HttpContext?
                    .Connection
                    .RemoteIpAddress?
                    .ToString(),

                UserAgent =
                    _httpContext.HttpContext?
                        .Request
                        .Headers["User-Agent"],

                TraceId =
                    Activity.Current?.TraceId
                        .ToString(),

                CreatedAt = DateTime.UtcNow
            };

            await _pipeline.EnqueueAsync(log);
        }
    }
}
