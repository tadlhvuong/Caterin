namespace Shared.Services.Email.EmailModels
{
    public sealed class ResetPasswordEmailModel : BaseEmailModel
    {
        /// <summary>
        /// Tên hiển thị của người dùng.
        /// </summary>
        public string UserName { get; init; } = string.Empty;

        /// <summary>
        /// Liên kết đặt lại mật khẩu.
        /// </summary>
        public string ResetPasswordUrl { get; init; } = string.Empty;

        /// <summary>
        /// Thời gian hiệu lực của liên kết (phút).
        /// </summary>
        public int ExpireMinutes { get; init; }

        /// <summary>
        /// Tên website/ứng dụng.
        /// </summary>
        public string SiteName { get; init; } = string.Empty;

        /// <summary>
        /// URL logo.
        /// </summary>
        public string LogoUrl { get; init; } = string.Empty;

        /// <summary>
        /// Hotline hỗ trợ.
        /// </summary>
        public string SupportPhone { get; init; } = string.Empty;
    }
}
