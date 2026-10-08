using Shared.DTOs.Chat;
using Shared.Requests.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Interfaces.Chat
{
    public interface IChatLabelService
    {
        Task<ChatLabelDto> CreateLabelAsync(CreateChatLabelRequest request, CancellationToken cancellationToken = default);

        Task<List<ChatLabelDto>> GetLabelsAsync(CancellationToken cancellationToken = default);

        Task<bool> DeleteLabelAsync(int labelId, CancellationToken cancellationToken = default);

        Task<ChatContactLabelDto?> AssignConversationLabelAsync(long conversationId, int labelId, CancellationToken cancellationToken = default);
        
        Task<bool> RemoveConversationLabelAsync(long conversationId, int labelId, CancellationToken cancellationToken = default);
    }
}
