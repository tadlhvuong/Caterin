using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Chat
{
    public class ChatRecentOrderDto
    {
        public int Id { get; set; }

        public string OrderCode { get; set; } = null!;

        public OrderStatus Status { get; set; }

        public PaymentStatus PaymentStatus { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
