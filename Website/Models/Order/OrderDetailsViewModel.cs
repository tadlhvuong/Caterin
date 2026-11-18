using System.ComponentModel.DataAnnotations;

namespace Website.Models.Order
{
    public class OrderDetailsViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Order status
        public string Status { get; set; } = "Pending";
        public string StatusLabel { get; set; } = "Chờ xác nhận";
        public string StatusColor { get; set; } = "warning";

        // Payment
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentMethodLabel { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Pending";
        public string PaymentStatusLabel { get; set; } = "Chưa thanh toán";

        // Shipping
        public string? CarrierName { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? EstimatedDeliveryAt { get; set; }

        // Customer
        public string RecipientName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string ShippingAddress { get; set; } = string.Empty;
        public string? ShippingNote { get; set; }

        // Amounts
        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        // Actions
        public bool CanCancel { get; set; }
        public bool CanReview { get; set; }
        public bool CanRequestReturn { get; set; }

        public string? CustomerNote { get; set; }
        public int ReviewableItemCount { get; set; }
        public int ReviewedItemCount { get; set; }
        public List<OrderItemViewModel> Items { get; set; } = [];
        public List<OrderTimelineItemViewModel> Timeline { get; set; } = [];
        public ReturnRequestSummaryViewModel? ReturnRequest { get; set; }
    }

    public class OrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string? VariantName { get; set; }
        public string? ImageUrl { get; set; }

        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class OrderTimelineItemViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? OccurredAt { get; set; }

        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }

        // Tên icon Phosphor, ví dụ: ph-check, ph-package, ph-truck
        public string Icon { get; set; } = "ph-package";
    }

    public class OrderTrackingViewModel
    {
        public string? OrderCode { get; set; }
        public string? Contact { get; set; }

        public OrderDetailsViewModel? Order { get; set; }

        public string? ErrorMessage { get; set; }
    }

    public class PaymentResultViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public string? TransactionCode { get; set; }

        // Success, Failed, Pending
        public string Result { get; set; } = "Pending";

        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        public DateTime? PaidAt { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public class MyOrdersViewModel
    {
        public string? Keyword { get; set; }
        public string? StatusFilter { get; set; }

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int InProgressOrders { get; set; }
        public int CompletedOrders { get; set; }
        public List<OrderListItemViewModel> Orders { get; set; } = [];
    }

    public class OrderListItemViewModel
    {
        public string OrderCode { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; } = "Pending";
        public string StatusLabel { get; set; } = "Chờ xác nhận";
        public string StatusColor { get; set; } = "warning";

        public string PaymentMethodLabel { get; set; } = string.Empty;
        public string PaymentStatusLabel { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = "Unpaid";
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }

        public string? FirstProductName { get; set; }
        public string? FirstProductImage { get; set; }

        public bool CanCancel { get; set; }
        public int ReviewableItemCount { get; set; }

        public int ReviewedItemCount { get; set; }
        public bool CanReview { get; set; }

    }
    public class CancelOrderViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lý do hủy đơn hàng.")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Lý do hủy không được vượt quá 500 ký tự.")]
        public string? OtherReason { get; set; }
    }
}
