using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Shared.Configurations.JWT;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Seeders;
using Shared.DTOs.Auth;
using Shared.DTOs.Identity;
using Shared.Extensions;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Interfaces.Core;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.Interfaces.Media;
using Shared.Middlewares;
using Shared.Resources;
using Shared.Services;
using Shared.Services.Authentication;
using Shared.Services.Caches;
using Shared.Services.Customer;
using Shared.Services.Email;
using Shared.Services.Log;
using Shared.Services.Media;
using Shared.Services.Order;
using Shared.Services.Product;
using Shared.UserValidation.Interface;
using Shared.UserValidation.Sevices;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
var services = builder.Services;
var configuration = builder.Configuration;

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var connectionString = builder.Configuration.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
services.AddHttpContextAccessor();

services.AddSingleton<LogQueue>();
services.AddSingleton<ILogPipeline, LogPipeline>();

services.AddScoped<IActivityLogger, ActivityLogger>();
services.AddScoped<ISecurityLogger, SecurityLogger>();

services.Configure<LogWorkerOptions>(builder.Configuration.GetSection("LogWorker"));
services.AddHostedService<LogBackgroundWorker>();

services.AddScoped<AuditInterceptor>();

services.AddDbContext<AppDbContext>((sp, options) => {
    options.UseNpgsql(connectionString);
    options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
    //options.EnableSensitiveDataLogging();
    //options.LogTo(
    //   Console.WriteLine,
    //   new[]
    //   {
    //        DbLoggerCategory.Database.Command.Name
    //   },
    //   LogLevel.Information);
});
services.AddDatabaseDeveloperPageExceptionFilter();

services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    options.User.RequireUniqueEmail = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.AllowedForNewUsers = true;

    options.Tokens.EmailConfirmationTokenProvider = "EmailConfirmation";
}).AddEntityFrameworkStores<AppDbContext>()
    .AddTokenProvider<EmailConfirmationTokenProvider<AppUser>>("EmailConfirmation")
    .AddDefaultTokenProviders();
services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = configuration["Authentication:Google:ClientSecret"]!;
        options.SaveTokens = true;

        options.ClaimActions.MapJsonKey("picture", "picture");
    })
    .AddFacebook(options =>
    {
        options.AppId = configuration["Authentication:Facebook:AppId"]!;
        options.AppSecret = configuration["Authentication:Facebook:AppSecret"]!;

        options.SaveTokens = true;
    });
//setup timelife confirm email: 24h
services.Configure<EmailConfirmationTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

//setup timelife confirm còn lại như change pass, reset pass: 15'
services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromMinutes(15);
});

services.AddOptions<JwtSetting>().Bind(builder.Configuration.GetSection("JwtSetting"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.SecretKey), "Jwt SecretKey missing").ValidateOnStart();

services.AddScoped<IJwtService, JwtService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IRefreshTokenService, RefreshTokenService>();
services.AddScoped<IModuleService, ModuleService>();
services.AddScoped<IPermissionService, PermissionService>();

services.AddScoped<ICurrentUserService, CurrentUserService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IMenuService, MenuService>();
services.AddScoped<IEmailSender, EmailSender>();
services.AddScoped<IEmailTemplateService, EmailTemplateService>();
services.AddScoped<IEmailActionService, EmailActionService>();

services.AddScoped<IRoutePermissionService, RoutePermissionService>();
services.AddScoped<IRoutePermissionCache, RoutePermissionCache>();

services.AddScoped<IRefreshTokenValidator, RefreshTokenValidator>();
services.AddScoped<IPermissionVersionValidator, PermissionVersionValidator>();
services.AddScoped<Shared.UserValidation.Interface.ISecurityStampValidator, Shared.UserValidation.Sevices.SecurityStampValidator>();
services.AddScoped<IUserValidationService, UserValidationService>();
services.AddScoped<IUserStatusValidator, UserStatusValidator>();

services.Configure<MediaStorageOptions>(builder.Configuration.GetSection("AppSettings:MediaStorage"));
builder.Services.AddScoped<IMediaService, MediaService>();

services.AddScoped<IProductService, ProductService>();
services.AddScoped<IOrderService, OrderService>();
services.AddScoped<ICustomerService, CustomerService>();

var useRedis = builder.Configuration.GetValue<bool>("Cache:UseRedis");
if (useRedis)
    services.AddSingleton<IAppCache, RedisCache>();
else
    services.AddSingleton<IAppCache, AppMemoryCache>();

services.AddJwtConfiguration(builder.Configuration);
services.AddPermissionAuthorization();
services.AddControllersWithViews().AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource));
    }); ;
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    //app.UseExceptionHandler("/Home/Error");

    //app.UseExceptionHandler("/StatusCode/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseExceptionHandler("/StatusCode/500");
app.UseStatusCodePagesWithReExecute("/StatusCode/{0}");
var options = new ForwardedHeadersOptions
{
    ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto
};

options.KnownNetworks.Clear();
options.KnownProxies.Clear();

app.UseForwardedHeaders(options);

app.UseHttpsRedirection();  
app.UseStaticFiles();
app.UseRouting();

app.UseSecurityHeaders();

app.UseMiddleware<RefreshTokenMiddleware>();

app.UseAuthentication();


app.UseMiddleware<UserValidationMiddleware>();

app.UseMiddleware<AdminAccessMiddleware>();

app.UsePermissionMiddleware(); // use app.UseMiddleware<PermissionMiddleware>(); nếu không cần extension

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var servicesProvider = scope.ServiceProvider;
    var context = servicesProvider.GetRequiredService<AppDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(servicesProvider);
}
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/admin")
    && context.Request.Path.Value?.TrimEnd('/') == "/admin")
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Response.Redirect("/admin/dashboard");
        }
        else
        {
            context.Response.Redirect("/admin/auth/login");
        }

        return;
    }

    await next();
});
app.MapControllerRoute(
    name: "admin_area",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
);
app.MapControllerRoute(
name: "default",
pattern: "{controller=Home}/{action=Index}/{id?}");

app.Lifetime.ApplicationStarted.Register(() =>
{
    _ = Task.Run(async () =>
    {
        using var scope = app.Services.CreateScope();
        //var module = scope.ServiceProvider.GetRequiredService<IModuleService>();
        //await module.SyncModulesAsync();
        //var permission = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        //await permission.SyncPermissionsAsync();

        var service = scope.ServiceProvider.GetRequiredService<IRoutePermissionService>();

        await service.SyncAsync();
    });
});
//app.Run();

try
{
    Log.Information("Application starting");
    //var secret = CommonHelper.Generate();
    //Log.Information(secret);
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}