namespace Website.Areas.Admin.Models.User
{
    public class UserDetailsViewModel
    {
        public string Id { get; set; }
        public string CurrentTab { get; set; } = "personal";
        public string UserName { get; set; }
        public string Email { get; set; }
        public string AvatarUrl { get; set; }
        public bool ConfirmEmail { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool IsActive { get; set; }  
    }
}
