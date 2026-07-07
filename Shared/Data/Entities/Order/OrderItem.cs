using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Order
{
//    - Id
//- OrderId
//- ProductVariantId
//- ProductName
//- VariantName
//- Price
//- Quantity
//- Total
//- CreatedAt
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId {  get; set; }
        public int ProductVariantId { get; set;  }
        public string ProductName { get; set; }
        public string Variantname { get; set; }
        public double Price { get; set; }
        public int Quantity { get; set; }
        public double Total { get; set; }  
        public DateTime CreatedAt { get; set; }
    }
}
