using Shared.Data.Entities.Media;
using Shared.Enums.Chat;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Chat
{
    public class ChatPendingUpload
    {
        public long Id { get; set; }

        public long MediaFileId { get; set; }

        public string? UserId { get; set; }

        public long? GuestSessionId { get; set; }

        // Hash của token dùng cho nhóm upload tạm.
        // Null với upload theo luồng UserId/GuestSessionId hiện tại.
        [MaxLength(64)]
        public string? UploadTokenHash { get; set; }
        public ChatPendingUploadStatus Status { get; set; }
            = ChatPendingUploadStatus.Pending;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? AttachedAt { get; set; }

        public long? MessageId { get; set; }

        public MediaFile MediaFile { get; set; } = null!;

        public ChatGuestSession? GuestSession { get; set; }

        public ChatMessage? Message { get; set; }
    }
}
