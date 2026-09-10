using Shared.Data.Entities.Identity;
using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Order
{
    public class Order
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string OrderCode { get; set; } = null!;

        [Required]
        public OrderStatus Status { get; set; }
        [Required]
        public PaymentStatus PaymentStatus { get; set; }

        [Required]
        public decimal SubTotal { get; set; }

        [Required]
        public decimal DiscountAmount { get; set; }

        [Required]
        public decimal ShippingAmount { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime UpdatedAt { get; set; }

        public virtual AppUser User { get; set; } = null!;

        public virtual OrderAddress Address { get; set; } = null!;

        public virtual ICollection<OrderItem> Items { get; set; } = new HashSet<OrderItem>();

        public virtual ICollection<OrderHistory> Histories { get; set; } = new HashSet<OrderHistory>();
    }
}
