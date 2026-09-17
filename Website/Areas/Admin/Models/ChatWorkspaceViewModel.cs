using Shared.DTOs.Chat;

namespace Website.Areas.Admin.Models
{
    public class ChatWorkspaceViewModel
    {
        public long InboxId { get; set; }
        public List<ChatConversationListItemDto> Conversations { get; set; }  = new();
    }
}
