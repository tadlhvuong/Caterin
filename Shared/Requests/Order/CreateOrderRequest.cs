using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Requests.Order
{
    public class CreateOrderRequest
    {
        public string? UserId { get; set; }

        public List<CreateOrderItemRequest> Items { get; set; } = [];

        public CreateOrderAddressRequest ShippingAddress { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public int ProductVariantId { get; set; }
        public int WarehouseId { get; set; }

        public int Quantity { get; set; }
    }

    public class CreateOrderAddressRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string? Ward { get; set; }

        public string? District { get; set; }

        public string? Province { get; set; }
    }
}
