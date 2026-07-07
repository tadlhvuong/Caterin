using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.Data.Entities.Identity.Log;
using Shared.Services.Authentication;
using System.Reflection.Emit;

namespace Shared.Data.Context
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string,IdentityUserClaim<string>, 
        AppUserRole,
        IdentityUserLogin<string>,
        IdentityRoleClaim<string>,
        IdentityUserToken<string>>
    {
        public virtual DbSet<Setting> Settings { get; set; }
        public virtual DbSet<AuditLog> AuditLogs { get; set; }
        public virtual DbSet<SecurityLog> SecurityLogs { get; set; }
        public virtual DbSet<ActivityLog> ActivityLogs { get; set; }
        public virtual DbSet<Language> Languages { get; set; }
        public virtual DbSet<Currency> Currencies { get; set; }
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
        public virtual DbSet<Permission> Permissions { get; set; }
        public virtual DbSet<PermissionLog> PermissionLogs { get; set; }
        public virtual DbSet<CMSModule> CMSModules { get; set; }
        public virtual DbSet<CMSCatalog> CMSCatalogs { get; set; }
        public virtual DbSet<RolePermission> RolePermissions { get; set; }
        public virtual DbSet<Menu> Menus { get; set; }
        public virtual DbSet<RoutePermission> RoutePermissions { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
            
        }
    }
}
