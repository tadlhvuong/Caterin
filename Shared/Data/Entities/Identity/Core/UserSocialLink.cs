using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Data.Entities.Identity.Core
{
    public class UserSocialLink
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(30)]
        public string Provider { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string Url { get; set; } = null!;

        public AppUser User { get; set; } = null!;
    }
}
