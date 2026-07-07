using Shared.Data.Entities.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.UserValidation.DTOs
{
    public sealed class UserValidationResult
    {
        public bool Succeeded { get; }

        public UserValidationError Error { get; }

        private UserValidationResult(
            bool succeeded,
            UserValidationError error)
        {
            Succeeded = succeeded;
            Error = error;
        }

        public static UserValidationResult Success()
            => new(true, UserValidationError.None);

        public static UserValidationResult Fail(
            UserValidationError error)
            => new(false, error);
    }
}
