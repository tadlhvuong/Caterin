using Shared.Data.Entities.Identity.Core;
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
        /// <summary>
        /// Xác thực email
        /// </summary>
        /// <param name="emailAction"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult> ConfirmEmailAsync(EmailAction emailAction, CancellationToken cancellationToken = default);
        /// <summary>
        /// Xác thực lại email
        /// </summary>
        /// <param name="email"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult> ResendConfirmEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>
        /// Refresh access token bằng refresh token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Logout user và revoke refresh token
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
        /// <summary>
        /// Quên mật khẩu
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Đổi mật khẩu khi chưa đăng nhập
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult> ResetPasswordByTokenAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Đổi mật khẩu khi đã đăng nhập
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ServiceResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Kiểm tra email tồn tại
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        Task<bool> IsEmailExistsAsync(string email);
        /// <summary>
        /// Kiểm tra username tồn tại
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        Task<bool> IsUsernameExistsAsync(string username);
    }
}

