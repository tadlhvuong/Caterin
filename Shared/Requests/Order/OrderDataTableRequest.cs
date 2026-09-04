using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Order
{
    public sealed class OrderDataTableRequest
    {
        public int Draw { get; init; }

        public int Start { get; init; }

        public int Length { get; init; }

        public DataTableSearch? Search { get; init; }

        public List<DataTableOrder> Order { get; init; } = [];

        public OrderStatus? Status { get; init; }
    }

    public sealed class DataTableOrder
    {
        public int Column { get; init; }

        public string? Dir { get; init; }
    }
}
