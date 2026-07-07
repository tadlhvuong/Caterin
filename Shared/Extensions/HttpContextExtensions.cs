using Microsoft.AspNetCore.Http;
using Shared.UserValidation.DTOs;

namespace Shared.Extensions
{
    public static class HttpContextExtensions
    {
        private const string UserValidationContextKey = nameof(UserValidationContext);

        public static void SetUserValidationContext(this HttpContext context, UserValidationContext validationContext)
        {
            context.Items[UserValidationContextKey] = validationContext;
        }

        public static UserValidationContext? GetUserValidationContext(this HttpContext context)
        {
            return context.Items.TryGetValue(UserValidationContextKey, out var value)
                ? value as UserValidationContext : null;
        }
    }
}
