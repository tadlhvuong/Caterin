namespace Website.Areas.Admin.Models
{
    public class UserDetailsViewModel
    {
        public Guid Id { get; set; }
        public string CurrentTab { get; set; } = "personal";
    }
}
