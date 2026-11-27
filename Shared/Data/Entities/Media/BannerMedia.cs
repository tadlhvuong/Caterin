using Shared.Data.Entities.Marketing;
using Shared.Enums.Banner;

namespace Shared.Data.Entities.Media
{
    public class BannerMedia
    {
        public int Id { get; set; }

        public int BannerId { get; set; }

        // MediaFile
        public long? DesktopMediaFileId { get; set; }
        public long? MobileMediaFileId { get; set; }

        public BannerMediaType MediaType { get; set; }

        // Desktop layout (%)
        public decimal DesktopX { get; set; }
        public decimal DesktopY { get; set; }
        public decimal DesktopWidth { get; set; }
        public string? AltText { get; set; }
        // Mobile layout (%)
        public decimal MobileX { get; set; }
        public decimal MobileY { get; set; }
        public decimal MobileWidth { get; set; }

        public int SortOrder { get; set; }

        public Banner Banner { get; set; } = null!;

        public MediaFile? DesktopMediaFile { get; set; }
        public MediaFile? MobileMediaFile { get; set; }
    }
}
