using Shared.Data.Entities.Media;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatAttachment
    {
        public long Id { get; set; }

        public long MessageId { get; set; }

        public long? MediaFileId { get; set; }

        [MaxLength(500)]
        public string FileName { get; set; } = null!;

        [MaxLength(100)]
        public string ContentType { get; set; } = null!;

        public long FileSize { get; set; }

        public ChatMessage Message { get; set; } = null!;

        public MediaFile? MediaFile { get; set; }
    }
}
