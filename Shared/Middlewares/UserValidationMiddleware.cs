using Microsoft.AspNetCore.Http;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Middlewares
{
    public sealed class UserValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public UserValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IUserValidationService validationService)
        {
            
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await _next(context);
                return;
            }

            var validationContext = new UserValidationContext
            {
                Scenario = UserValidationScenario.AccessToken,
                Principal = context.User
            };

            var result = await validationService.ValidateAsync(validationContext);

            if (!result.Succeeded)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            context.Items["UserValidationContext"] =
                validationContext ; 

            await _next(context);
        }
    }
}
