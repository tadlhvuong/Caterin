namespace Shared.Services.Email
{
    public interface IEmailTemplateService
    {
        Task<string> RenderAsync<T>(string templateName, T model);
    }
}
