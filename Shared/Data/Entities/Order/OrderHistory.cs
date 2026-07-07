using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Order
{
//    - Id
//- OrderId
//- Status
//- Note
//- UpdatedBy
//- CreatedAt
    public class OrderHistory
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public EntityStatus Status { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
