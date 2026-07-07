using Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
