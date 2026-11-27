using Microsoft.EntityFrameworkCore;
using Shared.Data.Context;
using Shared.Data.Entities.Chat;
using Shared.Data.Entities.Identity.Core;

namespace Shared.Data.Seeders
{
    public static class SeedChatInbox
    {
        public static async Task SeedAsync(AppDbContext _dbContext)
        {
            if (await _dbContext.ChatInboxes.AnyAsync())
                return;

            await _dbContext.ChatInboxes.AddRangeAsync(
            [
                new ChatInbox
                {
                    Name = "Caterin",
                    ChannelType = Enums.Chat.ChatChannelType.Website,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            ]);

            await _dbContext.SaveChangesAsync();
        }
    }
}
