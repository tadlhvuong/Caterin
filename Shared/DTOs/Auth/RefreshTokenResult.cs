namespace Shared.DTOs.Auth
{
    public class RefreshTokenResult
    {
        public string RefreshToken { get; set; }
        public DateTime ExpireAt { get; set; }
    }
}
