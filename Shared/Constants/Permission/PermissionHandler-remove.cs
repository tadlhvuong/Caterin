using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Shared.Data.Context;
using Shared.Interfaces.AuthServices;
using Shared.UserValidation.DTOs;
using Shared.UserValidation.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Constants.Permission
{
    public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IUserValidationService _userValidationService;

        public PermissionHandler(
            IUserValidationService userValidationService)
        {
            _userValidationService = userValidationService;
        }

        protected override async Task HandleRequirementAsync(
     AuthorizationHandlerContext context,
     PermissionRequirement requirement)
        {
            if (context.Resource is not HttpContext httpContext)
                return;

            var validationContext =
                httpContext.Items["UserValidationContext"] as UserValidationContext;

            if (validationContext == null)
                return;

            if (await _userValidationService.HasPermissionAsync(
                    validationContext,
                    requirement.Permission))
            {
                context.Succeed(requirement);
            }
        }
    }
}
