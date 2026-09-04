using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Product
{
    public class ProductTagMapping
    {
        public int ProductId { get; set; }

        public int TagId { get; set; }

        public int DisplayOrder { get; set; }

        // Navigation
        public Product Product { get; set; } = null!;

        public ProductTag Tag { get; set; } = null!;
    }
}
