using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Email
{
    public interface IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message);
        public Task SendSmsAsync(string number, string message);
    }
}
