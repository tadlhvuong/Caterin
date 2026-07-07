using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Order
{
//    - Id
//- UserId
//- OrderCode
//- Status
//- Subtotal
//- DiscountAmount
//- ShippingAmount
//- TotalAmount
//- Note
//- CreatedAt
//- UpdatedAt
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string OrderCode { get; set; }
        public EntityStatus Status { get; set; }
        public double SubTotal { get; set; }
        public double DiscountAmount { get; set; }
        public double ShippingAmount { get; set; }
        public double TotalAmount { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
