namespace Shared.Services.Email.EmailModels
{
    public class ConfirmEmailModel : BaseEmailModel
    {
        public string ConfirmEmailUrl { get; set; } = default!;
        public int ExpireHours { get; set; }
    }
}
