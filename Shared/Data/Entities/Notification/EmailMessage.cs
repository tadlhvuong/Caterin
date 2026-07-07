using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Notification
{
//    - Id
//- ToEmail
//- Subject
//- Body
//- Status
//- RetryCount
//- SentAt
//- CreatedAt
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
