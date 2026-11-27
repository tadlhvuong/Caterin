using Shared.Enums.Banner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Responses.Banner
{
    public class BannerResponse
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public BannerLinkType LinkType { get; set; }

        public string? LinkValue { get; set; }

        public BannerPosition Position { get; set; }

        public int SortOrder { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? StartAt { get; set; }

        public DateTime? EndAt { get; set; }

        public BannerStatus Status { get; set; }

        public List<BannerMediaResponse> Medias { get; set; } = new();
    }

    public class BannerMediaResponse
    {
        public int Id { get; set; }

        public BannerMediaType MediaType { get; set; }

        public long? DesktopMediaFileId { get; set; }

        public string? DesktopUrl { get; set; }

        public long? MobileMediaFileId { get; set; }

        public string? MobileUrl { get; set; }

        public decimal DesktopX { get; set; }

        public decimal DesktopY { get; set; }

        public decimal DesktopWidth { get; set; }

        public decimal MobileX { get; set; }

        public decimal MobileY { get; set; }

        public decimal MobileWidth { get; set; }

        public string? AltText { get; set; }

        public int SortOrder { get; set; }
    }
}
