using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using Shared.Enums;
namespace Shared.Data.Entities.Identity.Log
{
    public class AuditLog : LogBase
    {
        [Required]
        public AuditActionType ActionType { get; set; }

        [Required]
        [MaxLength(100)]
        public string TableName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string RecordId { get; set; } = null!;

        public string? OldValues { get; set; }

        public string? NewValues { get; set; }

        public string? ChangedColumns { get; set; }
    }
}
