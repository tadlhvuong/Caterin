using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Authentication
{
    public class ModuleService : IModuleService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAppCache _cache;
        private readonly ILogger<ModuleService> _logger;
        private readonly EndpointDataSource _endpointDataSource;

        public ModuleService(
            AppDbContext dbContext,
            EndpointDataSource endpointDataSource,
            IAppCache cache,
            ILogger<ModuleService> logger)
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
                    .DistinctBy(x => x!.ControllerTypeInfo.FullName);

                foreach (var controller in controllers)
                {
                    // Chỉ sync Area Admin
                    if (!string.Equals(controller!.RouteValues["area"], "Admin",
                            StringComparison.OrdinalIgnoreCase))
                        continue;

                    var code = controller.ControllerName.ToLowerInvariant();

                    scannedCodes.Add(code);

                    if (!moduleLookup.TryGetValue(code, out var module))
                    {
                        _dbContext.CMSModules.Add(new CMSModule
                        {
                            Code = code,
                            Name = controller.ControllerName,
                            IsActive = true,
                            IsSystem = false
                        });

                        continue;
                    }

                    module.Name = controller.ControllerName;
                    module.IsActive = true;
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
