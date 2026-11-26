using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using System.Reflection;

namespace Shared.Services.Authentication
{
    public class ModuleService : IModuleService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAppCache _cache;
        private readonly EndpointDataSource _endpointDataSource;

        private readonly ILogger<ModuleService> _logger;

        public ModuleService(AppDbContext dbContext, EndpointDataSource endpointDataSource,
            IAppCache cache, ILogger<ModuleService> logger)
        {
            _dbContext = dbContext;
            _endpointDataSource = endpointDataSource;
            _cache = cache;
            _logger = logger;
        }
        public async Task SyncModulesAsync(CancellationToken cancellationToken = default)
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var modules = await _dbContext.CMSModules
                    .AsTracking()
                    .ToListAsync(cancellationToken);

                var moduleLookup = modules.ToDictionary(
                    x => x.Code,
                    StringComparer.OrdinalIgnoreCase);

                var scannedCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var controllers = _endpointDataSource.Endpoints
                    .OfType<RouteEndpoint>()
                    .Select(e => e.Metadata.GetMetadata<ControllerActionDescriptor>())
                    .Where(x => x != null)
                    .DistinctBy(x => x!.ControllerTypeInfo.FullName).ToList();

                foreach (var controller in controllers)
                {
                    // Chỉ sync Area Admin
                    if (!string.Equals(controller!.RouteValues["area"], "Admin",
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    //var code = controller.ControllerName.ToLowerInvariant();
                    var moduleAttribute = controller.ControllerTypeInfo.GetCustomAttribute<PermissionModuleAttribute>();

                    if (moduleAttribute == null)
                        continue;

                    var code = moduleAttribute.Module.Trim().ToLowerInvariant();
                    if (!scannedCodes.Add(code))
                    {
                        throw new InvalidOperationException(
                            $"Module Code '{code}' bị khai báo trùng.");
                    }

                    if (!moduleLookup.TryGetValue(code, out var module))
                    {
                        module = new CMSModule
                        {
                            Code = code,
                            Name = controller.ControllerName,
                            IsActive = true,
                            IsSystem = false
                        };
                        _dbContext.CMSModules.Add(module);
                        moduleLookup.Add(code, module);
                    }
                    else
                    {
                        module.IsActive = true;
                    }
                }

                // Disable module đã bị xóa khỏi source
                foreach (var module in modules.Where(x => !x.IsSystem))
                {
                    if (!scannedCodes.Contains(module.Code))
                    {
                        module.IsActive = false;
                    }
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }
}
