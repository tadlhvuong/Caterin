using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Order
{
    public class OrderAddress
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [Required]
        [MaxLength(200)]
        public string ReceiverName { get; set; } = null!;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Province { get; set; } = null!;

        [MaxLength(100)]
        public string? District { get; set; }

        [Required]
        [MaxLength(100)]
        public string Ward { get; set; } = null!;

        [Required]
        [MaxLength(500)]
        public string AddressLine { get; set; } = null!;

        public virtual Order Order { get; set; } = null!;
    }
}
