using Website.Models.Enum;

namespace Website.Models.Account
{
    public class VerifyEmailViewModel
    {
        public VerifyEmailStatus Status { get; set; }

        public string? Email { get; set; }

        public string? Message { get; set; }
    }
}
