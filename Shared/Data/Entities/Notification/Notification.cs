using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Notification
{
//    - Id
//- UserId
//- Title
//- Content
//- Type
//- ActionUrl
//- IsRead
//- ReadAt
//- CreatedAt
    public class Notification
    {
        public int Id { get; set;  }
        public string UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
        public string ActionUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
