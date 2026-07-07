
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Product
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public string Bardcode { get; set; }
        public double Price { get; set; }
        public double SalePrice { get; set; }
        public double CostPrice { get; set; }
        public double Weight { get; set; }
        public bool IsDeafault { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
