
using System.ComponentModel.DataAnnotations;

namespace Website.Models.Payment
{
    public class RetryPaymentViewModel
    {
        public string OrderCode { get; set; } = string.Empty;

        public DateTime OrderCreatedAt { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public string OrderStatusLabel { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;

        public string PaymentStatusLabel { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentMethodLabel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string SelectedPaymentMethod { get; set; } = string.Empty;

        public List<RetryPaymentMethodViewModel> PaymentMethods { get; set; } = [];
    }

    public class RetryPaymentMethodViewModel
    {
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public bool IsSelected { get; set; }
    }
}
