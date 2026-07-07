using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Notification
{
    public class NotificationDelivery
    {
        public int Id { get; set; }
        public int NotificationId { get; set; }
        public int Channel { get; set; }
        public EntityStatus Status { get; set; }
        public DateTime SendAt { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
