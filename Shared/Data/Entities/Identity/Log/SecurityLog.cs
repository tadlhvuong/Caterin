using Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace Shared.Data.Entities.Identity.Log
{
    public class SecurityLog : LogBase
    {
        [Required]
        public SecurityActionType ActionType { get; set; }

        [Required]
        public bool IsSuccess { get; set; }

        [MaxLength(500)]
        public string? Message { get; set; }

        [MaxLength(200)]
        public string? TargetUserId { get; set; }

        [MaxLength(200)]
        public string? Resource { get; set; }

        public string? MetadataJson { get; set; }
    }
}
