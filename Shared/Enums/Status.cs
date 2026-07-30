using Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Shared.Enums
{
    public enum EntityStatus
    {
        [Display(Name = "Pending", ShortName = "Chờ kích hoạt"), StatusCss("primary")]
        Pending,
        [Display(Name = "Active", ShortName = "Đang hoạt động"), StatusCss("success")]
        Active,
        [Display(Name = "Suspended", ShortName = "Đình chỉ"), StatusCss("warning")]
        Suspended,
        [Display(Name = "Deleted", ShortName = "Đã xóa"), StatusCss("danger")]
        Deleted,
    }
}
