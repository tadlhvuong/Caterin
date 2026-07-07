using Shared.Requests;
using Shared.Responses;

namespace Shared.Interfaces.AuthServices
{
    public interface IAuthService
    {

        /// <summary>
        /// Đăng nhập hệ thống
        /// </summary>
        Task<AuthResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        /// <summary>
        /// Refresh access token bằng refresh token
        /// </summary>
        Task<AuthResponse> RefreshTokenAsync(string refreshToken,
        CancellationToken cancellationToken = default);

        /// <summary>
        /// Logout user và revoke refresh token
        /// </summary>
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task ForgotPasswordAsync(ForgotPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task ResetPasswordByTokenAsync(ResetPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra email tồn tại
        /// </summary>
        Task<bool> IsEmailExistsAsync(string email);

        /// <summary>
        /// Kiểm tra username tồn tại
        /// </summary>
        Task<bool> IsUsernameExistsAsync(string username);
    }
}

