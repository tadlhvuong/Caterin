using Shared.Data.Entities.Identity;
using Shared.UserValidation.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.Interface
{
    public interface ISecurityStampValidator
    {
        Task<UserValidationResult> ValidateAsync(
            UserValidationContext context,
            AppUser user,
            CancellationToken cancellationToken = default);
    }
}
