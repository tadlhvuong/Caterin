using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Identity.Log
{
    public abstract class LogBase
    {
        [Key]
        public long Id { get; set; }

        [MaxLength(450)]
        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        [MaxLength(100)]
        public string? TraceId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
