using Shared.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Log
{
    public class ActivityLog : LogBase
    {
        [Required]
        public ActivityType ActivityType { get; set; }

        [MaxLength(200)]
        public string EntityType { get; set; } = null!;

        [MaxLength(100)]
        public string EntityId { get; set; } = null!;

        [MaxLength(500)]
        public string Description { get; set; } = null!;

        public string? MetadataJson { get; set; }
    }
}
