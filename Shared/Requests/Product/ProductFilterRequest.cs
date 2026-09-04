using Shared.Enums;
using Shared.Requests.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Product
{
    public class ProductFilterRequest
    {
        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;

        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public ProductStatus? Status { get; set; }

        public bool? IsFeatured { get; set; }

        public string? SortColumn { get; set; }

        public string? SortDirection { get; set; }
    }
}
