using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Log
{
    public interface ILogSafetyService
    {
        Task TryLogSecurityAsync(
            SecurityActionType action,
            bool succeeded,
            string message);

        Task TryLogActivityAsync(
            ActivityType type,
            string entityName,
            string entityId,
            string description);
    }
}
