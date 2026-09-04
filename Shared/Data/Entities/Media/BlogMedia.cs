using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Media
{
    public class BlogMedia
    {
        public int BlogId { get; set; }

        public long MediaFileId { get; set; }

        //public Blog Blog { get; set; } = null!;

        public MediaFile MediaFile { get; set; } = null!;
    }
}
