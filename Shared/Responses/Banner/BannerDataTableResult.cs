using Shared.DTOs.Banner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses.Banner
{
    public class BannerDataTableResult
    {
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }

        public List<BannerDataTableItemDto> Items { get; set; } = new();
    }
}
