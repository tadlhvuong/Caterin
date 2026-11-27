using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Banner
{
    public class BannerDataTableItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;

        public int Position { get; set; }
        public string PositionName { get; set; } = null!;

        public int LinkType { get; set; }

        public long? DesktopMediaFileId { get; set; }
        public long? MobileMediaFileId { get; set; }
        public string? MobileImageUrl { get; set; }
        public string? DesktopImageUrl { get; set; }
        public bool HasDesktop { get; set; }
        public bool HasMobile { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public int Status { get; set; }
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; }
    }
}
