using Shared.Helpers;
using System.ComponentModel.DataAnnotations;

namespace Shared.Enums
{
    public enum OrderStatus
    {
        [Display(Name = "Pending", ShortName = "Chờ xác nhận"), StatusCss("warning")]
        Pending = 1,
        [Display(Name = "Confirmed", ShortName = "Đã xác nhận"), StatusCss("info")]
        Confirmed,
        [Display(Name = "Processing", ShortName = "Đang xử lý"), StatusCss("primary")]
        Processing,
        [Display(Name = "Shipping", ShortName = "Đang giao"), StatusCss("info")]
        Shipping,
        [Display(Name = "Completed", ShortName = "Hoàn thành"), StatusCss("success")]
        Completed,
        [Display(Name = "Cancelled", ShortName = "Đã hủy"), StatusCss("danger")]
        Cancelled,
        [Display(Name = "Refunded", ShortName = "Đã hoàn tiền"), StatusCss("secondary")]
        Refunded
    }

    public enum DeliveryStatus
    {
        Pending =1,
        Shipped,
        OutForDelivery,
        Delivered,
        Failed,
    }
}
