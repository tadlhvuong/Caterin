using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Payment
{
//    - Id
//- Provider
//- EventType
//- Payload
//- IsProcessed
//- CreatedAt
    public class WebhookLog
    {
        public int Id { get; set; }
        public string Provider { get; set; }
        public string EventType { get; set; }
        public string Payload { get; set; }
        public bool IsProcessed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
