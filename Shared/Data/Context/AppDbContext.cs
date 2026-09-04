using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shared.Data.Entities.Catelog;
using Shared.Data.Entities.Identity;
using Shared.Data.Entities.Identity.Core;
using Shared.Data.Entities.Identity.Log;
using Shared.Data.Entities.Inventory;
using Shared.Data.Entities.Media;
using Shared.Data.Entities.Order;
using Shared.Data.Entities.Product;
using Shared.Services.Authentication;
using System.Reflection.Emit;
using Attribute = Shared.Data.Entities.Product.Attribute;

namespace Shared.Data.Context
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string,IdentityUserClaim<string>,  AppUserRole,
        IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>
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
        public virtual DbSet<EmailAction> EmailActions { get; set; }

        public virtual DbSet<MediaFile> MediaFiles { get; set; }

        public virtual DbSet<Attribute> Attributes { get; set; }
        public virtual DbSet<AttributeValue> AttributeValues { get; set; }
        public virtual DbSet<VariantAttribute> VariantAttributes { get; set; }

        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ProductCategory> ProductCategories { get; set; }
        public virtual DbSet<ProductVariant> ProductVariants { get; set; }
        public virtual DbSet<ProductVariantMedia> ProductVariantMedias { get; set; }
        public virtual DbSet<ProductMedia> ProductMedias { get; set; }
        public virtual DbSet<ProductTag> ProductTags { get; set; }
        public virtual DbSet<ProductTagMapping> ProductTagMappings { get; set; }

        public virtual DbSet<Order> Orders { get; set; }
        public virtual DbSet<OrderItem> OrderItems { get; set; }
        public virtual DbSet<OrderAddress> OrderAddresses { get; set; }
        public virtual DbSet<OrderHistory> OrderHistories { get; set; }

        public virtual DbSet<Warehouse> Warehouses { get; set; }
        public virtual DbSet<InventoryStock> InventoryStocks { get; set; }
        public virtual DbSet<InventoryTransaction> InventoryTransactions { get; set; }

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
