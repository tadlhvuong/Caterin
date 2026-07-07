
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Marketing
{
    //    - Id
    //- EventId
    //- ProductVariantId
    //- SalePrice
    //- QuantityLimit
    //- SoldQuantity
    //- CreatedAt

    public class FlashSaleItem
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public int ProductVariantId {get;set;}
        public double SalePrice { get; set; }
        public int QuantityLimit { get; set;}
        public int SoldQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
