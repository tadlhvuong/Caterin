using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Interfaces.Chat;

namespace Shared.Services.Chat
{
    public class ChatAuthorizationService : IChatAuthorizationService
    {
        private readonly AppDbContext _dbContext;

        private readonly ILogger<ChatAuthorizationService> _logger;

        public ChatAuthorizationService(AppDbContext dbContext, ILogger<ChatAuthorizationService> logger)
        {
            _dbContext = dbContext;

            _logger = logger;
        }

        public async Task<bool> CanAccessConversationAsync(long conversationId, long? contactId, string? userId, bool isAdmin, CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == conversationId, cancellationToken);

            if (conversation == null)
                return false;

            // =========================
            // ADMIN
            // =========================
            if (isAdmin)
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return false;

                // Phase 1:
                // Admin đã đăng nhập thì được phép truy cập.
                //
                // Sau khi nối với Permission system:
                // Chat.View / Chat.Reply
                // sẽ kiểm tra ở đây.

                return true;
            }

            // =========================
            // CUSTOMER / GUEST
            // =========================

            if (!contactId.HasValue)
                return false;

            // Conversation phải thuộc Contact
            if (conversation.ContactId != contactId.Value)
                return false;

            // Nếu contact là user đã đăng nhập
            // thì kiểm tra thêm UserId.
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var contact = await _dbContext.ChatContacts.AsNoTracking().FirstOrDefaultAsync(x => x.Id == contactId.Value, cancellationToken);

                if (contact == null)
                    return false;

                // Contact đã liên kết AppUser khác
                if (!string.IsNullOrWhiteSpace(contact.UserId) && contact.UserId != userId)
                {
                    return false;
                }
            }

            return true;
        }

        public async Task<bool> CanCustomerAccessConversationAsync(long conversationId, long? contactId, string? userId, string? guestToken, CancellationToken cancellationToken = default)
        {
            var conversation = await _dbContext.ChatConversations.AsNoTracking().Where(x => x.Id == conversationId)
                    .Select(x => new
                    {
                        x.ContactId,
                        ContactUserId = x.Contact.UserId,
                        ContactIsMerged = x.Contact.IsMerged
                    }).FirstOrDefaultAsync(cancellationToken);

            if (conversation == null)
            {
                return false;
            }

            // =========================================================
            // AUTHENTICATED CUSTOMER
            // =========================================================

            if (!string.IsNullOrWhiteSpace(userId))
            {
                // Authenticated customer không cần GuestToken.
                //
                // Contact phải là canonical Contact của UserId.
                if (!contactId.HasValue)
                {
                    return false;
                }

                if (conversation.ContactId != contactId.Value)
                {
                    return false;
                }

                if (conversation.ContactIsMerged)
                {
                    return false;
                }

                return conversation.ContactUserId == userId;
            }

            // =========================================================
            // GUEST
            // =========================================================

            if (!contactId.HasValue || string.IsNullOrWhiteSpace(guestToken))
            {
                return false;
            }

            if (conversation.ContactId != contactId.Value)
            {
                return false;
            }

            if (conversation.ContactIsMerged)
            {
                return false;
            }

            var tokenHash = Common.CommonHelper.Hash(guestToken);

            var now = DateTime.UtcNow;

            return await _dbContext.ChatGuestSessions.AsNoTracking().AnyAsync(
                    x => x.ContactId == contactId.Value && x.TokenHash == tokenHash && !x.IsRevoked && x.ExpiresAt > now, cancellationToken);
        }
    }
}
