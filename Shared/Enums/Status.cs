using Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Shared.Enums
{
    public enum EntityStatus
    {
        [Display(Name = "None", ShortName = "Không rõ"), StatusCss("default")]
        None,
        [Display(Name = "Enabled", ShortName = "Hoạt động"), StatusCss("success")]
        Enabled,
        [Display(Name = "Disabled", ShortName = "Khóa"), StatusCss("danger")]
        Disabled,
        [Display(Name = "Expiried", ShortName = "Hết hạn"), StatusCss("warning")]
        Expiried,
        [Display(Name = "Testing", ShortName = "Thử nghiệm"), StatusCss("info")]
        Testing,
    }
}
