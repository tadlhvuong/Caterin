using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses.Chat
{
    public class ChatAttachmentUploadResponse
    {
        public long UploadId { get; init; }
        public string? FileName { get; init; }
        public string? ContentType { get; init; }
        public long FileSize { get; init; }
        public DateTime ExpiresAt { get; init; }
        public string? UploadSessionToken { get; init; }
    }
}
