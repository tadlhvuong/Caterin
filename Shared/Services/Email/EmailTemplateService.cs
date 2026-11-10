using Microsoft.AspNetCore.Hosting;
using Shared.Services.Email;
using System.Text.Encodings.Web;
using System.Threading;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IWebHostEnvironment _env;

    public EmailTemplateService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<string> RenderAsync<T>(string templateName, T model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateName);
        ArgumentNullException.ThrowIfNull(model);
        var path = Path.Combine(AppContext.BaseDirectory, "Services", "Email", "Templates", "Account", $"{templateName}.html");

        var html = await File.ReadAllTextAsync(path);

        foreach (var property in typeof(T).GetProperties())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var value = property.GetValue(model)?.ToString() ?? "";
            value = HtmlEncoder.Default.Encode(value);

            html = html.Replace($"{{{{{property.Name}}}}}", value);
        }

        return html;
    }
}