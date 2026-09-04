using Shared.Enums;

namespace Website.Areas.Admin.Models.Order
{
    public sealed class OrderListItemViewModel
    {
        public int Id { get; init; }

        public string OrderCode { get; init; } = string.Empty;

        public OrderStatus   Status { get; init; }

        public decimal TotalAmount { get; init; }

        public int ItemCount { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
