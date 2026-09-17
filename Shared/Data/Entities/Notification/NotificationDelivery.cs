using Shared.Enums;

namespace Shared.Data.Entities.Notification
{
    public class NotificationDelivery
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public NotificationChannel Channel { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime? SendAt { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public Notification Notification { get; set; } = null!;
    }
}
