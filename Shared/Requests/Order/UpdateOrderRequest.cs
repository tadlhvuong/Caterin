using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Order
{
    public class UpdateOrderRequest
    {
        public decimal ShippingFee { get; set; }

        public decimal DiscountAmount { get; set; }

        public UpdateOrderAddressRequest? ShippingAddress { get; set; }
    }

    public class UpdateOrderAddressRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Ward { get; set; }

        public string? District { get; set; }

        public string? Province { get; set; }
    }
}
