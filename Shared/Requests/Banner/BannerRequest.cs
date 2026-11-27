using Shared.Enums.Banner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Banner
{
    public class BannerRequest
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

        public List<BannerMediaRequest> Medias { get; set; } = new();
    }
}
