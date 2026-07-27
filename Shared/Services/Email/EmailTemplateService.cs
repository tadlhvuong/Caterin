using Microsoft.AspNetCore.Hosting;
using Shared.Services.Email;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _env;

    public EmailTemplateService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> RenderAsync<T>(string templateName, T model)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Services", "Email", "Templates", "Account", $"{templateName}.html");

        var html = await File.ReadAllTextAsync(path);

        foreach (var property in typeof(T).GetProperties())
        {
            var value = property.GetValue(model)?.ToString() ?? "";

            html = html.Replace($"{{{{{property.Name}}}}}", value);
        }

        return html;
    }
}