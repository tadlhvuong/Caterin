namespace Shared.DTOs.Chat
{
    public class ChatAttachmentDto
    {
        public long Id { get; set; }
        public string FileName { get; set; } = null!;
        public string ContentType { get; set; } = null!;
        public long FileSize { get; set; }

        // URL do server tạo từ StoragePath đã được kiểm tra.
        public string? Url { get; set; }
    }
}
