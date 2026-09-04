using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Order;

namespace Website.Areas.Admin.Models.Order
{
    public class OrderDetailsViewModel
    {
        public int OrderId { get; set; }
        public string UserId { get; set; }
        public string OrderName { get; set; } = string.Empty;
        public int OrderStatus { get; set; }
        public string OrderStatusDescription { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal total { get; set; }
        public ICollection<OrderItem> Items { get; set; }
        public AppUser User { get; set; }
        public OrderAddress Address { get; set; }
    }
}
