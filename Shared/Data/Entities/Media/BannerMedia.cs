using Shared.Data.Entities.Marketing;

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
