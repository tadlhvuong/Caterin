using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;
using Shared.Common;
using Shared.Configurations.JWT;
using Shared.Constants.JWT;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.Data.Seeders;
using Shared.DTOs.Auth;
using Shared.Extensions;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Caches;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using Shared.Middlewares;
using Shared.Services;
using Shared.Services.Authentication;
using Shared.Services.Caches;
using Shared.Services.Email;
using Shared.Services.Log;
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
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

//services.Configure<JwtSetting>(builder.Configuration.GetSection("JwtSetting"));
services.AddOptions<JwtSetting>().Bind(builder.Configuration.GetSection("JwtSetting"))
    .Validate(s => !string.IsNullOrWhiteSpace(s.SecretKey), "Jwt SecretKey missing").ValidateOnStart();

services.AddScoped<IJwtService, JwtService>();
services.AddScoped<IAuthService, AuthService>();
services.AddScoped<IRefreshTokenService, RefreshTokenService>();
services.AddScoped<IPermissionService, PermissionService>();

services.AddScoped<ICurrentUserService, CurrentUserService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IMenuService, MenuService>();
services.AddScoped<IEmailSender, EmailSender>();

services.AddScoped<IRoutePermissionService, RoutePermissionService>();
services.AddScoped<IRoutePermissionCache, RoutePermissionCache>();

services.AddScoped<IRefreshTokenValidator, RefreshTokenValidator>();
services.AddScoped<IPermissionVersionValidator, PermissionVersionValidator>();
services.AddScoped<Shared.UserValidation.Interface.ISecurityStampValidator, Shared.UserValidation.Sevices.SecurityStampValidator>();
services.AddScoped<IUserValidationService, UserValidationService>();
services.AddScoped<IUserStatusValidator, UserStatusValidator>();

var useRedis = builder.Configuration.GetValue<bool>("Cache:UseRedis");
if (useRedis)
    services.AddSingleton<IAppCache, RedisCache>();
else
    services.AddSingleton<IAppCache, AppMemoryCache>();

services.AddJwtConfiguration(builder.Configuration);
services.AddPermissionAuthorization();
services.AddControllersWithViews();
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

    app.UseExceptionHandler("/StatusCode/500");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
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