using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    using Shared.Enums;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class EmailAction
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// SHA256(Key)
        /// Không lưu key gốc.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string KeyHash { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// ConfirmEmail, ResetPassword...
        /// </summary>
        [Required]
        public EmailActionType Type { get; set; }

        /// <summary>
        /// Identity Token (Base64UrlEncode)
        /// </summary>
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiredAt { get; set; }

        public DateTime? UsedAt { get; set; }

        public DateTime? RevokedAt { get; set; }

        [MaxLength(500)]
        public string? RevokedReason { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual AppUser User { get; set; } = default!;
    }
}
