using Shared.Data.Entities.Marketing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Media
{
    public class BannerMedia
    {
        public int BannerId { get; set; }

        public long MediaFileId { get; set; }

        public Banner Banner { get; set; } = null!;

        public MediaFile MediaFile { get; set; } = null!;
    }
}
