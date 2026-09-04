using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Order
{
    public class OrderDetailsDto
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public OrderStatus Status { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public decimal SubTotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public OrderAddressDto? ShippingAddress { get; set; }

        public List<OrderItemDto> Items { get; set; } = [];
    }
}