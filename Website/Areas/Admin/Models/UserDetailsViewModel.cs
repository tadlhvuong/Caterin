namespace Website.Areas.Admin.Models
{
    public class UserDetailsViewModel
    {
        public string Id { get; set; }
        public string CurrentTab { get; set; } = "personal";
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool ConfirmEmail { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }
}
