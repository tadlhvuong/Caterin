
using System.ComponentModel.DataAnnotations;

namespace Website.Models.Order;

public class ReturnRequestViewModel
{
    public string OrderCode { get; set; } = string.Empty;

    public DateTime? ReceivedAt { get; set; }

    public DateTime? ReturnDeadline { get; set; }

    public string RecipientName { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;

    public List<ReturnRequestItemViewModel> Items { get; set; } = [];

    [Required(ErrorMessage = "Vui lòng chọn hình thức xử lý.")]
    public string RequestType { get; set; } = "Refund";

    [Required(ErrorMessage = "Vui lòng chọn lý do đổi trả.")]
    public string Reason { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
    public string? Description { get; set; }
}

public class ReturnRequestItemViewModel
{
    public string ProductName { get; set; } = string.Empty;

    public string? VariantName { get; set; }

    public string? ImageUrl { get; set; }

    public decimal UnitPrice { get; set; }

    public int PurchasedQuantity { get; set; }

    public bool IsSelected { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "Số lượng không hợp lệ.")]
    public int ReturnQuantity { get; set; }
}
