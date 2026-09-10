using Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Shared.Enums
{
    public enum ProductStatus
    {
        [Display(Name = "Draft", ShortName = "Bản nháp")]
        [StatusCss("secondary")]
        Draft = 0,

        [Display(Name = "Active", ShortName = "Đang bán")]
        [StatusCss("success")]
        Active = 1,

        [Display(Name = "Inactive", ShortName = "Ngừng bán")]
        [StatusCss("warning")]
        Inactive = 2,

        [Display(Name = "Archived", ShortName = "Lưu trữ")]
        [StatusCss("secondary")]
        Archived = 3
    }
}
