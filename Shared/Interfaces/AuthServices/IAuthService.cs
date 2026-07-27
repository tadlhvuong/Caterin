using Shared.Requests;
using Shared.Responses;

namespace Shared.Interfaces.AuthServices
{
    public interface IAuthService
    {

        /// <summary>
        /// Đăng nhập hệ thống
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đăng nhập: Facebook / Google ...
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthResponse> ExternalLoginAsync(ExternalLoginRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Đăng ký tài khoản mới
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);

        Task<ServiceResult> ConfirmEmailAsync(ConfirmEmailRequest request);
        /// <summary>
        /// Refresh access token bằng refresh token
        /// </summary>
        Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logout user và revoke refresh token
        /// </summary>
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);

        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<ServiceResult> ResetPasswordByTokenAsync(ResetPasswordRequest request,
            CancellationToken cancellationToken = default);

        Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request,
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

