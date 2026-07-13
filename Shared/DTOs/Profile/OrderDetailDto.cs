using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Profile
{
    public class OrderDetailDto
    {
        public Guid Id { get; set; }

        public string OrderCode { get; set; } = default!;

        public DateTime OrderDate { get; set; }

        public string OrderStatus { get; set; } = default!;

        public string PaymentStatus { get; set; } = default!;

        public string PaymentMethod { get; set; } = default!;

        public decimal Subtotal { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal Discount { get; set; }

        public decimal TotalAmount { get; set; }

        //public ShippingAddressDto ShippingAddress { get; set; } = default!;

        public List<OrderItemDto> Items { get; set; } = [];
    }
}
