using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Payment
{
//    - Id
//- OrderId
//- PaymentMethodId
//- Status
//- Amount
//- TransactionCode
//- PaidAt
//- CreatedAt
    public class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int PaymentMethodId { get; set; }
        public EntityStatus Status { get; set; }
        public double Amount { get; set; }
        public string TransactionCode { get; set; }
        public DateTime PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
