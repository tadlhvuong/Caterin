using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Marketing
{
//    - Id
//- CouponId
//- UserId
//- OrderId
//- UsedAt
    public class CouponUsage
    {
        public int Id { get; set; }
        public int CouponId { get; set; }
        public string UserId { get; set; }
        public int OrderId { get; set; }
        public DateTime UsedAt { get; set; }
    }
}
