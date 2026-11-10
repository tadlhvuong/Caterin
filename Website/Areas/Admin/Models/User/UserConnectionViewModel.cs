namespace Website.Areas.Admin.Models.User
{
    public class UserConnectionViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public List<ExternalLoginProviderViewModel> Providers { get; set; } = new();
    }

    public class ExternalLoginProviderViewModel
    {
        public string Provider { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        public bool IsConnected { get; set; }
    }
}
