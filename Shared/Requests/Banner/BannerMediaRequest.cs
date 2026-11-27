using Microsoft.AspNetCore.Http;
using Shared.Enums.Banner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Banner
{
    public class BannerMediaRequest
    {
        public int Id { get; set; }

        public BannerMediaType MediaType { get; set; }

        public long? DesktopMediaFileId { get; set; }

        public long? MobileMediaFileId { get; set; }

        public IFormFile? DesktopFile { get; set; }

        public IFormFile? MobileFile { get; set; }

        public bool RemoveDesktopImage { get; set; }
        public bool RemoveMobileImage { get; set; }

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
