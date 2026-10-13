using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses.Chat
{
    public sealed class ChatAttachmentReadResponse : IDisposable
    {
        public required Stream Stream { get; init; }
        public required string ContentType { get; init; }
        public required string FileName { get; init; }

        public void Dispose() => Stream.Dispose();
    }
}
