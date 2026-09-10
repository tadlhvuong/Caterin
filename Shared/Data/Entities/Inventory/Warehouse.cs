using Shared.Data.Entities.Order;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Inventory
{
    public class Warehouse
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(30)]
        public string? Phone { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<OrderItem> OrderItems { get; set; } = [];
        public ICollection<InventoryStock> InventoryStocks { get; set; } = [];
        public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = [];
    }
}
