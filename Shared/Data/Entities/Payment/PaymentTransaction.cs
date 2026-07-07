using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Payment
{
//    - Id
//- PaymentId
//- Gateway
//- TransactionId
//- RawResponse
//- IsSuccess
//- CreatedAt
    public  class PaymentTransaction
    {
        public int Id { get; set; }
        public int PaymentId { get; set; }
        public int TransactionId { get; set; }
        public string Gateway { get; set; }
        public string RawResponse { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
