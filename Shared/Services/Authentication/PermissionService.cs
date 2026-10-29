using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity.Core;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Services.Caches;

namespace Shared.Services.Authentication
{
    public sealed class PermissionService : IPermissionService
    {
        private readonly AppDbContext _dbContext;
        private readonly IAppCache _cache;
        private readonly CacheSettings _cacheSettings;

        private readonly ILogger<PermissionService> _logger;

        public PermissionService(AppDbContext dbContext, IAppCache cache, IOptions<CacheSettings> cacheSettings, ILogger<PermissionService> logger)
        {
            _dbContext = dbContext;
            _cache = cache;
            _cacheSettings = cacheSettings.Value;

            _logger = logger;
        }

        #region Sync / Generate Permissions

        public async Task GeneratePermissionsAsync(int moduleId, bool saveChanges = true)
        {
            var module = await _dbContext.CMSModules.FirstOrDefaultAsync(x => x.Id == moduleId);

            if (module == null)
            {
                _logger.LogError("Module {ModuleId} not found.", moduleId);
                throw new Exception($"Module {moduleId} not found.");
            }

            var actions = await _dbContext.CMSCatalogs.Where(x => x.Type == CatalogType.Action && x.IsActive).ToListAsync();

            if (actions.Count == 0)
                return;

            var existingCodes = await _dbContext.Permissions.Where(x => x.ModuleId == moduleId && !x.IsSystem).Select(x => x.Code).ToListAsync();

            var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

            var newPermissions = new List<Permission>();

            foreach (var action in actions)
            {
                var code = $"{module.Code}.{action.Code}".ToLowerInvariant();

                if (existingSet.Contains(code))
                    continue;

                newPermissions.Add(new Permission
                {
                    ModuleId = module.Id,
                    Code = code,
                    Name = $"{module.Name} {action.Name}",
                    Action = action.Name,
                    Description = $"{action.Name} permission for {module.Name}",
                    IsActive = true,
                    IsSystem = false
                });
            }

            if (newPermissions.Count == 0)
                return;

            await _dbContext.Permissions.AddRangeAsync(newPermissions);

            if (saveChanges)
            {
                await _dbContext.SaveChangesAsync();
                await InvalidatePermissionLookupCacheAsync();

                _logger.LogInformation("Generated {PermissionCount} permissions for module {ModuleId}.",
                    newPermissions.Count, moduleId);
            }
        }

        public async Task SyncPermissionsAsync()
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync();

            try
            {
                var modules = await _dbContext.CMSModules.Where(x => x.IsActive).ToListAsync();

                foreach (var module in modules)
                {
                    if (!module.IsSystem)
                    {
                        await GeneratePermissionsAsync(module.Id, saveChanges: false);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                await InvalidatePermissionLookupCacheAsync();

                _logger.LogInformation("Permission synchronization completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission sync failed");
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion

        #region Query / Lookup
        public async Task<int?> GetPermissionIdAsync(string permissionCode)
        {
            var lookup = await _cache.GetOrCreateAsync(CacheKeys.PermissionCodeLookup(),
                async () =>
                {
                    return await _dbContext.Permissions
                        .AsNoTracking()
                        .Where(x => x.IsActive)
                        .ToDictionaryAsync(
                            x => x.Code,
                            x => x.Id,
                            StringComparer.OrdinalIgnoreCase);
                },
                TimeSpan.FromMinutes(_cacheSettings.PermissionCodeLookupMinutes));

            if (lookup == null)
                return null;

            return lookup.TryGetValue(permissionCode, out var id)
                ? id
                : null;
        }

        public async Task<UserPermissionSnapshot> GetUserPermissionSnapshotAsync(string userId, long permissionVersion,
             CancellationToken cancellationToken = default)
        {
            var cacheKey = CacheKeys.UserPermissionSnapshot(userId, permissionVersion);
            Console.WriteLine(
        $"[PermissionCache] GET | UserId={userId} | Version={permissionVersion} | Key={cacheKey}");
            return await _cache.GetOrCreateAsync(cacheKey, async () =>
               {
                   Console.WriteLine(
                       $"[PermissionCache] MISS | UserId={userId} | Version={permissionVersion} | Building snapshot");

                   var snapshot = await BuildSnapshotAsync(
                       userId, cancellationToken);

                   Console.WriteLine(
                       $"[PermissionCache] BUILT | UserId={userId} | Version={permissionVersion} | PermissionCount={snapshot.PermissionIds.Count} | IsRoot={snapshot.IsRoot}");

                   return snapshot;
               }, TimeSpan.FromMinutes(_cacheSettings.UserPermissionSnapshotMinutes));
            //return await _cache.GetOrCreateAsync(
            //    CacheKeys.UserPermissionSnapshot(userId, permissionVersion),
            //    () => BuildSnapshotAsync(userId, cancellationToken),
            //    TimeSpan.FromMinutes(_cacheSettings.UserPermissionSnapshotMinutes));
        }

        #endregion

        #region Cache Utilities
        private async Task InvalidatePermissionLookupCacheAsync()
        {
            await _cache.RemoveAsync(CacheKeys.PermissionCodeLookup());
        }

        private async Task<UserPermissionSnapshot> BuildSnapshotAsync(string userId, CancellationToken cancellationToken)
        {
            var permissionIds = await
            (
                from ur in _dbContext.UserRoles.AsNoTracking()
                join rp in _dbContext.RolePermissions.AsNoTracking()
                    on ur.RoleId equals rp.RoleId
                join p in _dbContext.Permissions.AsNoTracking()
                    on rp.PermissionId equals p.Id
                where ur.UserId == userId
                        && p.IsActive
                select rp.PermissionId
            )
            .Distinct().ToListAsync(cancellationToken);

            var rootPermissionId = await GetPermissionIdAsync(PermissionCodes.SystemRoot);

            var isRoot = rootPermissionId != null && permissionIds.Contains(rootPermissionId.Value);

            return new UserPermissionSnapshot
            {
                PermissionIds = permissionIds.ToHashSet(),
                IsRoot = isRoot
            };
        }
        public async Task<bool> IsRootAsync(
    string userId,
    long permissionVersion,
    CancellationToken cancellationToken = default)
        {
            var snapshot = await GetUserPermissionSnapshotAsync(
                userId,
                permissionVersion,
                cancellationToken);

            return snapshot.IsRoot;
        }
        #endregion
    }
}
