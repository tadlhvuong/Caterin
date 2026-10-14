using System.ComponentModel.DataAnnotations;

namespace Shared.Requests.Chat
{
    public class CreateChatLabelRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? Color { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
