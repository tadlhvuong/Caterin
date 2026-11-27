using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Banner
{
    public class BannerPublishRequest
    {
        public int Id { get; set; }
        public bool IsPublished { get; set; }
    }
}
