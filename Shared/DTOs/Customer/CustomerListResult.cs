using Shared.Enums;

namespace Shared.DTOs.Customer
{
    public class CustomerListResult
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public int OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime? LastOrderAt { get; set; }

        public EntityStatus Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
