using Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Shared.Enums
{
    public enum PaymentStatus
    {
        [Display(Name = "Pending", ShortName = "Chờ xác nhận"), StatusCss("warning")]
        Pending = 0,

        [Display(Name = "Processing", ShortName = "Đang xử lý"), StatusCss("info")]
        Processing = 1,

        [Display(Name = "Paid", ShortName = "Đã thanh toán"), StatusCss("success")]
        Paid = 2,

        [Display(Name = "Failed", ShortName = "Thanh toán thất bại"), StatusCss("danger")]
        Failed = 3,

        [Display(Name = "Cancelled", ShortName = "Đã hủy"), StatusCss("secondary")]
        Cancelled = 4,

        [Display(Name = "Refunded", ShortName = "Đã hoàn tiền"), StatusCss("primary")]
        Refunded = 5,

        [Display(Name = "PartiallyRefunded", ShortName = "Hoàn tiền một phần"), StatusCss("primary")]
        PartiallyRefunded = 6
    }
}
