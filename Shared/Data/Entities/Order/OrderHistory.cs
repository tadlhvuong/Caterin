using Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Order
{
    public class OrderHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public virtual Order Order { get; set; } = null!;
    }
}
