using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Order
{
    public class OrderListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }
        public OrderStatus? Status { get; set; }
        public string? SortColumn { get; init; }
        public bool SortDescending { get; init; }
    }
}
