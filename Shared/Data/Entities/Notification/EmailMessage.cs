using Shared.Enums;

namespace Shared.Data.Entities.Notification
{
    public class EmailMessage
    {
        public int Id { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public EntityStatus Status { get; set; }
        public int RetryCount { get; set; }
        public DateTime SendAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
