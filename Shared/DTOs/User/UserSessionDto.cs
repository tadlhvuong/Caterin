namespace Shared.DTOs.User
{
    public class UserSessionDto
    {
        public Guid Id { get; set; }
        public string? DeviceName { get; set; }
        public string? UserAgent { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastActivityAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsCurrent { get; set; }
    }
}
