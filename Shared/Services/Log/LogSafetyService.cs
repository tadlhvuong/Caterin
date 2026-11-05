using Microsoft.Extensions.Logging;
using Shared.Enums;
using Shared.Interfaces.Log;

namespace Shared.Services.Log
{
    public sealed class LogSafetyService : ILogSafetyService
    {
        private readonly ISecurityLogger _securityLogger;
        private readonly IActivityLogger _activityLogger;
        private readonly ILogger<LogSafetyService> _logger;

        public LogSafetyService(
            ISecurityLogger securityLogger,
            IActivityLogger activityLogger,
            ILogger<LogSafetyService> logger)
        {
            _securityLogger = securityLogger;
            _activityLogger = activityLogger;
            _logger = logger;
        }

        public async Task TryLogSecurityAsync(
            SecurityActionType action,
            bool succeeded,
            string message)
        {
            try
            {
                await _securityLogger.LogAsync(
                    action,
                    succeeded,
                    message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                     "Failed to enqueue security log. Action: {Action}, Success: {Success}",
                    action, succeeded);
            }
        }

        public async Task TryLogActivityAsync(
            ActivityType type,
            string entityName,
            string entityId,
            string description)
        {
            try
            {
                await _activityLogger.LogAsync(
                    type,
                    entityName,
                    entityId,
                    description);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to enqueue activity log. Type: {ActivityType}, Entity: {EntityName}, EntityId: {EntityId}",
                    type,
                    entityName,
                    entityId);
            }
        }
    }
}
