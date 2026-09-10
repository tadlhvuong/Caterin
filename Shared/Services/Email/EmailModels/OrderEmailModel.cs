namespace Shared.Services.Email.EmailModels
{
    public class OrderEmailModel : BaseEmailModel
    {
        public string CustomerName { get; set; }

        public string OrderCode { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; }

        public string OrderDetailUrl { get; set; }


        // Shipping
        public string ShippingAddress { get; set; }

        public string TrackingCode { get; set; }

        public string ShippingProvider { get; set; }


        // Cancel
        public string CancelReason { get; set; }


        // Refund
        public decimal RefundAmount { get; set; }

        public string RefundMethod { get; set; }

        public DateTime? RefundDate { get; set; }

        public string RefundReason { get; set; }
    }
}
