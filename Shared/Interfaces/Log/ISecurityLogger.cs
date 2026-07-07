using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Log
{
    public interface ISecurityLogger
    {
        /// <summary>
        /// Lưu lịch sử bảo mật hệ thống
        /// </summary>
        Task LogAsync(SecurityActionType action, bool success, string message);
    }}
