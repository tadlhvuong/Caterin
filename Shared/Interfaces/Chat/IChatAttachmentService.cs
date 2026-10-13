using Microsoft.AspNetCore.Http;
using Shared.Responses.Chat;

namespace Shared.Interfaces.Chat
{
    public interface IChatAttachmentService
    {
        Task<ChatAttachmentUploadResponse> UploadAsync(
        IFormFile file,
        long? contactId,
        string? guestToken,
        string? uploadSessionToken,
        string? userId,
        CancellationToken cancellationToken = default);

        Task<ChatAttachmentReadResponse?> OpenForAdminDownloadAsync(
    long attachmentId,
    CancellationToken cancellationToken = default);

        Task<ChatAttachmentReadResponse?> OpenForAdminPreviewAsync(
            long attachmentId,
            CancellationToken cancellationToken = default);

        Task<ChatAttachmentReadResponse?> OpenForDownloadAsync(
    long attachmentId,
    long? contactId,
    string? guestToken,
    string? userId,
    CancellationToken cancellationToken = default);

        Task<ChatAttachmentReadResponse?> OpenForPreviewAsync(
            long attachmentId,
            long? contactId,
            string? guestToken,
            string? userId,
            CancellationToken cancellationToken = default);
    }
}
