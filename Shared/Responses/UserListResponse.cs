using Shared.Enums;

namespace Shared.Responses
{
    public class UserListResponse
    {
        public string Id { get; set; } = default!;
        public string Avatar { get; set; } = default!;
        public string FullName { get; set; } = default!;

        public string Email { get; set; } = default!;

        //public bool EmailConfirmed { get; set; }

        //public bool Locked { get; set; }
        public string Billing { get; set; } = default!;
        public List<string> Roles { get; set; } = [];
        public EntityStatus Status { get; set; }

        public DateTime? CreatedAt { get; set; }
    }
}
