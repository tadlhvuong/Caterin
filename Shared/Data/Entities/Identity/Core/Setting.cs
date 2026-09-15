using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Identity.Core
{
    public class Setting
    {
        public long Id { get; set; }
        [Required]
        [MaxLength(150)]
        public string Key { get; set; } = null!;
        [Required]
        [MaxLength(50)]
        public SettingGroup Group { get; set; }
        public string? Value { get; set; } = null!;

        public string? Description { get; set; }

        public bool IsPublic { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
