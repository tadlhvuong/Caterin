using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Data.Context;
using Shared.Data.Entities.Chat;
using Shared.DTOs.Chat;
using Shared.Interfaces.Chat;
using Shared.Requests.Chat;

namespace Shared.Services.Chat
{
    public class ChatLabelService : IChatLabelService
    {
        private readonly AppDbContext _dbContext;

        private readonly ILogger<ChatLabelService> _logger;

        public ChatLabelService(AppDbContext dbContext, ILogger<ChatLabelService> logger)
        {
            _dbContext = dbContext;

            _logger = logger;
        }

        public async Task<ChatLabelDto> CreateLabelAsync(CreateChatLabelRequest request, CancellationToken cancellationToken = default)
        {
            var name = request.Name.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Tên label không được để trống.");
            }

            var exists = await _dbContext.ChatLabels.AnyAsync(x => x.Name == name, cancellationToken);

            if (exists)
            {
                throw new InvalidOperationException($"Label '{name}' đã tồn tại.");
            }

            var label = new ChatLabel
            {
                Name = name,
                Color = string.IsNullOrWhiteSpace(request.Color) ? "#7367F0" : request.Color.Trim(),
                IsActive = request.IsActive
            };

            _dbContext.ChatLabels.Add(label);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ChatLabelDto
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color,
                IsActive = label.IsActive
            };
        }

        public async Task<List<ChatLabelDto>> GetLabelsAsync(CancellationToken cancellationToken = default)
        {
            return await _dbContext.ChatLabels.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name)
                .Select(x => new ChatLabelDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Color = x.Color,
                    IsActive = x.IsActive
                }).ToListAsync(cancellationToken);
        }

        public async Task<bool> DeleteLabelAsync(int labelId, CancellationToken cancellationToken = default)
        {
            var label = await _dbContext.ChatLabels.FirstOrDefaultAsync(x => x.Id == labelId, cancellationToken);

            if (label == null)
            {
                return false;
            }

            label.IsActive = false;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

        public async Task<ChatContactLabelDto?> AssignConversationLabelAsync(long conversationId, int labelId, CancellationToken cancellationToken = default)
        {
            var conversationExists = await _dbContext.ChatConversations.AnyAsync(x => x.Id == conversationId, cancellationToken);

            if (!conversationExists)
            {
                return null;
            }

            var label = await _dbContext.ChatLabels.AsNoTracking().FirstOrDefaultAsync(x => x.Id == labelId && x.IsActive, cancellationToken);

            if (label == null)
            {
                throw new InvalidOperationException("Label không tồn tại hoặc đã bị vô hiệu hóa.");
            }

            var exists = await _dbContext.ChatConversationLabels.AnyAsync(x => x.ConversationId == conversationId && x.LabelId == labelId, cancellationToken);

            if (exists)
            {
                return new ChatContactLabelDto
                {
                    Id = label.Id,
                    Name = label.Name,
                    Color = label.Color
                };
            }

            var conversationLabel = new ChatConversationLabel
            {
                ConversationId = conversationId,
                LabelId = labelId
            };

            _dbContext.ChatConversationLabels.Add(conversationLabel);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new ChatContactLabelDto
            {
                Id = label.Id,
                Name = label.Name,
                Color = label.Color
            };
        }

        public async Task<bool> RemoveConversationLabelAsync(long conversationId, int labelId, CancellationToken cancellationToken = default)
        {
            var conversationLabel = await _dbContext.ChatConversationLabels.FirstOrDefaultAsync(x => x.ConversationId == conversationId && x.LabelId == labelId, cancellationToken);

            if (conversationLabel == null)
            {
                return false;
            }

            _dbContext.ChatConversationLabels.Remove(conversationLabel);

            await _dbContext.SaveChangesAsync(cancellationToken);

            return true;
        }

    }
}
