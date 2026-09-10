using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;

namespace Shared.Constants.Permission
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IUserValidationService _userValidationService;

        public PermissionHandler(IUserValidationService userValidationService)
        {
            _userValidationService = userValidationService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
        {
            if (context.Resource is not HttpContext httpContext)
                return;

            var validationContext = httpContext.Items["UserValidationContext"] as UserValidationContext;

            if (validationContext == null)
                return;

            if (await _userValidationService.HasPermissionAsync(validationContext, requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}
