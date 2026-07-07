
using Microsoft.AspNetCore.Identity;
using Shared.Data.Entities.Identity;
using Shared.Enums;

public static class SeedUsers
{
    public static async Task SeedAsync(UserManager<AppUser> userManager)
    {
        const string username = "root";

        var user = await userManager.FindByNameAsync(username);

        if (user != null)
            return;

        user = new AppUser
        {
            UserName = username,
            NormalizedUserName = username.ToUpper(),
            Email = "root@local",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = EntityStatus.Enabled,
            IsActive = true
        };

        var result = await userManager.CreateAsync(user, "Root@123456");
    }
}