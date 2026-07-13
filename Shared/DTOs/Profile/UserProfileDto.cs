using Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.Profile
{
    public class UserProfileDto
    {
        public string? Id { get; set; } = null!;

        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Avatar { get; set; }

        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
        public EntityStatus Status { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool SavedSuccessfully { get; set; } = false;
    }
}
