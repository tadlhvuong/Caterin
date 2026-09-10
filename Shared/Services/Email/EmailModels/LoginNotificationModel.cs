namespace Shared.Services.Email.EmailModels
{
    public class LoginNotificationModel : BaseEmailModel
    {
        public DateTime LoginTime { get; set; }

        public string DeviceName { get; set; }

        public string Browser { get; set; }

        public string IpAddress { get; set; }

        public string LoginUrl { get; set; }
    }
}
