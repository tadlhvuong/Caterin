using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Log
{
    public interface IActivityLogger
    {
        /// <summary>
        /// Lưu lịch sử hoạt động người dùng
        /// </summary>
        Task LogAsync(ActivityType type, string entityType, string entityId, string description);
    }
}
