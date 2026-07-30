using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests
{
    public class UserQueryRequest
    {
        public string? Keyword { get; set; }

        public bool? EmailConfirmed { get; set; }

        public bool? Locked { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;
        public int? Status { get; set; }
        public string? Role { get; set; }
    }
}
