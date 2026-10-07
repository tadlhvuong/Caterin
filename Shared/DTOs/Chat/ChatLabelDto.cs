namespace Shared.DTOs.Chat
{
    public class ChatLabelDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Color { get; set; }
        public bool IsActive { get; set; }
    }
}
