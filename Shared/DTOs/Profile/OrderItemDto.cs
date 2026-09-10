namespace Shared.DTOs
{
    public class OrderItemDto
    {
        public Guid Id { get; set; }

        public string OrderCode { get; set; } = default!;

        public DateTime OrderDate { get; set; }

        public int TotalItems { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatus { get; set; } = default!;

        public string PaymentStatus { get; set; } = default!;

        public string PaymentMethod { get; set; } = default!;

        public string Thumbnail { get; set; } = default!;
    }
}
