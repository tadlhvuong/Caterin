using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Common;
using Shared.Data.Context;
using Shared.Data.Entities.Chat;
using Shared.Data.Entities.Media;
using Shared.DTOs.Identity;
using Shared.Enums.Chat;
using Shared.Interfaces.Chat;
using Shared.Interfaces.Media;
using Shared.Responses.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Chat
{
    public class ChatAttachmentService : IChatAttachmentService
    {
        private readonly AppDbContext _dbContext;
        private readonly IChatAuthorizationService _chatAuthorizationService;
        private readonly IMediaService _mediaService;
        private readonly MediaStorageOptions _mediaStorage;

        private readonly ILogger<ChatAttachmentService> _logger;

        public ChatAttachmentService(AppDbContext dbContext, IChatAuthorizationService chatAuthorizationService, 
            IMediaService mediaService, IOptions<MediaStorageOptions> mediaStorage,
            ILogger<ChatAttachmentService> logger)
        {
            _dbContext = dbContext;
            _chatAuthorizationService = chatAuthorizationService;
            _mediaService = mediaService;
            _mediaStorage = mediaStorage.Value;

            _logger = logger;
        }
        public async Task<ChatAttachmentUploadResponse> UploadAsync(IFormFile file, long? contactId,
            string? guestToken, string? uploadSessionToken, string? userId, CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length <= 0)
            {
                throw new ArgumentException("Vui lòng chọn file hợp lệ.", nameof(file));
            }

            var now = DateTime.UtcNow;

            long? guestSessionId = null;
            string? uploadTokenHash = null;
            string? issuedUploadSessionToken = null;

            // 1. Xác định danh tính và quyền upload.
            if (!string.IsNullOrWhiteSpace(userId))
            {
                // User đăng nhập: sở hữu upload theo UserId.
                if (!string.IsNullOrWhiteSpace(uploadSessionToken))
                {
                    throw new ArgumentException(
                        "Token upload tạm không áp dụng cho tài khoản đăng nhập.");
                }
            }
            else
            {
                var hasContactId = contactId.HasValue && contactId.Value > 0;
                var hasGuestToken = !string.IsNullOrWhiteSpace(guestToken);

                // Chỉ chấp nhận đủ cả hai thông tin hoặc không có cả hai.
                if (hasContactId != hasGuestToken ||
                    (contactId.HasValue && contactId.Value <= 0))
                {
                    throw new UnauthorizedAccessException(
                        "Phiên trò chuyện không hợp lệ.");
                }

                if (hasContactId && hasGuestToken)
                {
                    // 2. Guest đã có session: xác minh guest token.
                    var tokenHash = CommonHelper.Hash(guestToken!);

                    var guestSession = await _dbContext.ChatGuestSessions
                        .AsNoTracking()
                        .Where(x =>
                            x.ContactId == contactId!.Value &&
                            !x.IsRevoked &&
                            x.ExpiresAt > now &&
                            x.TokenHash == tokenHash)
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (guestSession == null)
                    {
                        throw new UnauthorizedAccessException(
                            "Phiên trò chuyện không hợp lệ.");
                    }

                    if (!string.IsNullOrWhiteSpace(uploadSessionToken))
                    {
                        throw new ArgumentException(
                            "Không cần token upload tạm cho phiên này.");
                    }

                    guestSessionId = guestSession.Id;
                }
                else
                {
                    // 3. Guest mới: chưa có contact và guest session.
                    if (string.IsNullOrWhiteSpace(uploadSessionToken))
                    {
                        // Tạo token mới cho nhóm upload tạm.
                        issuedUploadSessionToken =
                            CommonHelper.GenerateSecureToken(32);

                        uploadTokenHash =
                            CommonHelper.Hash(issuedUploadSessionToken);
                    }
                    else
                    {
                        // Tiếp tục nhóm upload tạm đã tồn tại.
                        uploadTokenHash =
                            CommonHelper.Hash(uploadSessionToken);

                        var hasActiveUpload =
                            await _dbContext.ChatPendingUploads
                                .AsNoTracking()
                                .AnyAsync(x =>
                                    x.UploadTokenHash == uploadTokenHash &&
                                    x.UserId == null &&
                                    x.GuestSessionId == null &&
                                    x.Status == ChatPendingUploadStatus.Pending &&
                                    x.ExpiresAt > now,
                                    cancellationToken);

                        if (!hasActiveUpload)
                        {
                            throw new UnauthorizedAccessException(
                                "Phiên upload không hợp lệ hoặc đã hết hạn.");
                        }
                    }
                }
            }

            // 4. Lưu file vật lý và tạo các bản ghi database.
            MediaFile? mediaFile = null;
            var fileSavedToDisk = false;

            // Commit đã bắt đầu nhưng kết quả không xác định:
            // không xóa file ngay vì database có thể đã commit.
            var commitAttempted = false;

            try
            {
                mediaFile = await _mediaService.UploadAsync(file, "Chat", cancellationToken);

                fileSavedToDisk = true;

                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync(
                        cancellationToken);

                var pendingUpload = new ChatPendingUpload
                {
                    MediaFile = mediaFile,
                    UserId = userId,
                    GuestSessionId = guestSessionId,
                    UploadTokenHash = uploadTokenHash,
                    Status = ChatPendingUploadStatus.Pending,
                    CreatedAt = now,
                    ExpiresAt = now.AddMinutes(30)
                };

                _dbContext.MediaFiles.Add(mediaFile);
                _dbContext.ChatPendingUploads.Add(pendingUpload);

                await _dbContext.SaveChangesAsync(cancellationToken);

                // Từ thời điểm này, nếu CommitAsync gặp lỗi,
                // trạng thái commit có thể không xác định.
                commitAttempted = true;

                await transaction.CommitAsync(cancellationToken);

                // 5. Trả kết quả sau khi commit thành công.
                return new ChatAttachmentUploadResponse
                {
                    UploadId = pendingUpload.Id,
                    FileName = mediaFile.OriginalFileName,
                    ContentType = mediaFile.ContentType,
                    FileSize = mediaFile.Size,
                    ExpiresAt = pendingUpload.ExpiresAt,
                    UploadSessionToken = issuedUploadSessionToken
                };
            }
            catch (Exception ex)
            {
                // Chỉ cleanup khi chưa bắt đầu commit.
                // Nếu commit đã bắt đầu, giữ file để tránh xóa nhầm
                // file mà database có thể đã tham chiếu.
                if (fileSavedToDisk &&
                    !commitAttempted &&
                    mediaFile != null)
                {
                    try
                    {
                        await _mediaService.DeleteAsync(
                            mediaFile,
                            CancellationToken.None);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(
                            cleanupEx,
                            "Không thể dọn file chat sau khi upload thất bại.");
                    }
                }

                // Không biến cancellation thành lỗi upload thông thường.
                if (ex is OperationCanceledException &&
                    cancellationToken.IsCancellationRequested)
                {
                    throw;
                }

                _logger.LogError(
                    ex,
                    "Không thể upload file đính kèm chat.");

                throw;
            }
        }



        public Task<ChatAttachmentReadResponse?> OpenForAdminDownloadAsync(
    long attachmentId,
    CancellationToken cancellationToken = default)
        {
            return OpenAttachmentAsync(
        attachmentId,
        contactId: null,
        guestToken: null,
        userId: null,
        previewOnly: false,
        cancellationToken: cancellationToken,
        isAdmin: true);
        }

        public Task<ChatAttachmentReadResponse?> OpenForAdminPreviewAsync(
            long attachmentId,
            CancellationToken cancellationToken = default)
        {
            return OpenAttachmentAsync(
        attachmentId,
        contactId: null,
        guestToken: null,
        userId: null,
        previewOnly: true,
        cancellationToken: cancellationToken,
        isAdmin: true);
        }

        public Task<ChatAttachmentReadResponse?> OpenForDownloadAsync(
    long attachmentId,
    long? contactId,
    string? guestToken,
    string? userId,
    CancellationToken cancellationToken = default)
        {
            return OpenAttachmentAsync(
                attachmentId, contactId, guestToken, userId,
                previewOnly: false, cancellationToken);
        }

        public Task<ChatAttachmentReadResponse?> OpenForPreviewAsync(long attachmentId, long? contactId,
            string? guestToken, string? userId, CancellationToken cancellationToken = default)
        {
            return OpenAttachmentAsync(
                attachmentId, contactId, guestToken, userId,
                previewOnly: true, cancellationToken);
        }

        private async Task<ChatAttachmentReadResponse?> OpenAttachmentAsync(long attachmentId, long? contactId, string? guestToken,
            string? userId, bool previewOnly, CancellationToken cancellationToken, bool isAdmin = false)
        {
            var attachment = await _dbContext.ChatAttachments.AsNoTracking().Include(x => x.Message)
                .Include(x => x.MediaFile).FirstOrDefaultAsync(
                    x => x.Id == attachmentId,
                    cancellationToken);

            if (attachment?.Message == null ||
                attachment.MediaFile == null ||
                attachment.Message.IsPrivate)
            {
                return null;
            }

            if (!isAdmin)
            {
                var allowed =
                    await _chatAuthorizationService.CanCustomerAccessConversationAsync(
                        attachment.Message.ConversationId,
                        contactId,
                        userId,
                        guestToken,
                        cancellationToken);

                if (!allowed)
                    return null;
            }

            var contentType = attachment.MediaFile.ContentType?
                .Trim()
                .ToLowerInvariant();

            if (previewOnly)
            {
                var allowedImageTypes = new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "image/gif"
        };

                if (string.IsNullOrWhiteSpace(contentType) ||
                    !allowedImageTypes.Contains(contentType))
                {
                    return null;
                }
            }

            var stream = await _mediaService.OpenReadAsync(
                attachment.MediaFile,
                cancellationToken);

            if (stream == null)
                return null;

            var fileName = Path.GetFileName(
                (attachment.FileName ?? string.Empty).Replace('\\', '/'));

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"attachment-{attachment.Id}";

            return new ChatAttachmentReadResponse
            {
                Stream = stream,
                ContentType = previewOnly ? contentType! : "application/octet-stream",
                FileName = fileName
            };
        }

    }
}
