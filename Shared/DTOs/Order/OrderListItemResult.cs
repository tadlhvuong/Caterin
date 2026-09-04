using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Order
{
    public sealed class OrderListItemResult
    {
        public int Id { get; init; }

        public string Order { get; init; } = string.Empty;

        public DateTime Date { get; init; }

        public string Time { get; init; } = string.Empty;

        public string Customer { get; init; } = string.Empty;

        public string? Email { get; init; }

        public string? Avatar { get; init; }

        public PaymentStatus PaymentStatus { get; init; }

        public OrderStatus Status { get; init; }

        public string? Method { get; init; }

        public string? MethodNumber { get; init; }

        public decimal TotalAmount { get; init; }

        public int ItemCount { get; init; }
    }
}
