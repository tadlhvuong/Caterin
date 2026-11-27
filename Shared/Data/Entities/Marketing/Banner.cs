using Shared.Data.Entities.Media;
using Shared.Enums.Banner;

namespace Shared.Data.Entities.Marketing
{
    public class Banner
    {
        public int Id { get; set; } 
        public string Title { get; set; }
        public BannerLinkType LinkType { get; set; }
        public string? LinkValue { get; set; }
        public BannerPosition Position { get; set; }
        public int SortOrder { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public ICollection<BannerMedia> BannerMedias { get; set; } = new List<BannerMedia>();
    }
}
