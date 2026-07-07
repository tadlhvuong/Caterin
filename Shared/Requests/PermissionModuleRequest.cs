using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests
{
    public sealed class CreateModuleRequest
    {
        public string Code { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string? Icon { get; set; }

        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public List<long> ActionIds { get; set; } = [];
    }
}
