namespace Shared.Services.Email.EmailModels
{
    public class SuccessEmailModel : BaseEmailModel
    {
        public string LoginUrl { get; set; }

        public string NewEmail { get; set; }
    }
}
