using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product
{
    public sealed class UpdateProductStockRequest
    {
        public int Id { get; set; }
        public bool InStock { get; set; }
    }
}
