using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Enums.Banner
{
    public enum BannerPosition
    {
        [Display(Name = "Banner chính trang chủ")]
        HomeHero = 1,
        [Display(Name = "Banner giữa trang chủ")]
        HomeMiddle = 2,
        [Display(Name = "Banner cuối trang chủ")]
        HomeBottom = 3,
        [Display(Name = "Banner đầu cửa hàng")]
        ShopTop = 4,
        [Display(Name = "Banner đầu bài viết")]
        BlogTop = 5
    }
}
