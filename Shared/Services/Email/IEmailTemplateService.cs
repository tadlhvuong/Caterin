using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email
{
    public interface IEmailTemplateService
    {
        Task<string> RenderAsync<T>(string templateName, T model);
    }
}
