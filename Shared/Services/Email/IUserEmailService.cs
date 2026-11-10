using Shared.Data.Entities.Identity;
using Shared.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shared.Services.Email
{
    public interface IUserEmailService
    {
        Task<ServiceResult> SendConfirmEmailAsync(AppUser user, CancellationToken cancellationToken = default);
        Task SendWelcomeEmailAsync(AppUser user, CancellationToken cancellationToken = default);
        Task<ServiceResult> SendForgotPasswordEmailAsync(AppUser usertionToken, CancellationToken cancellationToken = default);
    }
}
