using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Shared.Data.Entities.Identity;
using Shared.Enums;

public static class SeedUsers
{
    public static async Task SeedAsync(UserManager<AppUser> userManager, IConfiguration configuration)
    {
        const string username = "root";

        var existingUser = await userManager.FindByNameAsync(username);

        if (existingUser != null)
            return;
        var password = configuration["Caterin:RootUser:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Missing configuration: Caterin:RootUser:Password");
        }
        var user = new AppUser
        {
            UserName = username,
            Email = "root@local",
            EmailConfirmed = true,

            DisplayName = "Root",
            CreatedAt = DateTime.UtcNow,
            Status = EntityStatus.Active,
            IsActive = true,
            IsDeleted = false,
            IsLocked = false
        };

        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(e => $"{e.Code}: {e.Description}"));

            throw new InvalidOperationException(
                $"Failed to create root user: {errors}");
        }
    }
}