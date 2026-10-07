using Shared.Data.Entities.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public sealed class GuestIdentityResult
    {
        public ChatContact Contact { get; init; } = null!;

        /// <summary>
        /// Plaintext token chỉ có giá trị khi server vừa tạo GuestSession mới.
        /// Không bao giờ lưu plaintext token vào DB.
        /// </summary>
        public string? GuestToken { get; init; }

        public bool IsNewContact { get; init; }
    }
}
