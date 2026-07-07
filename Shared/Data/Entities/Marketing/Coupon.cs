using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Marketing
{
//    - Id
//- Code
//- Name
//- Description
//- DiscountValue
//- IsPercentage
//- MaxDiscountAmount
//- MinimumOrderAmount
//- UsageLimit
//- UsedCount
//- StartDate
//- EndDate
//- IsActive
//- CreatedAt
    public class Coupon
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double DiscountValue { get; set; }
        public bool IsPercentage { get; set; }
        public double MaxDiscountAmount { get; set; }
        public double MinimumOrderAmount { get; set; }
        public int UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
