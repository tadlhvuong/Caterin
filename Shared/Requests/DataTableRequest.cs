using Shared.Requests.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests
{
    public class DataTableRequest
    {
        public int Draw { get; set; }

        public int Start { get; set; }

        public int Length { get; set; }

        public string? Search { get; set; } = null;

        public string? SortColumn { get; set; }

        public string? SortDirection { get; set; }
        public int? Status { get; set; }
        public int? Stock { get; set; }
    }
}
