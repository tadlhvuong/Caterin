using Shared.Enums;

namespace Shared.Interfaces.Log
{
    public interface ISecurityLogger
    {
        /// <summary>
        /// Lưu lịch sử bảo mật hệ thống
        /// </summary>
        Task LogAsync(SecurityActionType action, bool success, string message);
    }}
