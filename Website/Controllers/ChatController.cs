using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Common;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Chat;
using Shared.Data.Entities.Media;
using Shared.Enums;
using Shared.Enums.Chat;
using Shared.Interfaces.Chat;
using Shared.Interfaces.Media;
using Shared.Requests.Chat;
using System.Security.Claims;
using System.Xml.Linq;

namespace Website.Controllers
{
    [Route("chat")]
    public class ChatController : Controller
    {

        private readonly AppDbContext _dbContext;

        private readonly IChatAuthorizationService _chatAuthorizationService;
        private readonly IChatConversationService _chatConversationService;
        private readonly IChatMessageService _chatMessageService;
        private readonly IMediaService _mediaService;

        private readonly ILogger _logger;
        public ChatController(AppDbContext dbContext, IChatAuthorizationService chatAuthorizationService,
            IChatConversationService chatConversationService, IChatMessageService chatMessageService, IMediaService mediaService,
            ILogger<ChatController> logger)
        {
            _dbContext = dbContext;

            _chatAuthorizationService = chatAuthorizationService;
            _chatConversationService = chatConversationService;
            _chatMessageService = chatMessageService;
            _mediaService = mediaService;

            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartChatRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
            {
                return BadRequest("Invalid request.");
            }

            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;

            var result = await _chatConversationService.StartClientConversationAsync(request, userId, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{conversationId:long}/messages")]
        public async Task<IActionResult> GetConversationMessages(long conversationId, 
            [FromHeader(Name = "X-Chat-Contact-Id")] long contactId, [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
            [FromQuery] int limit = 30, [FromQuery] long? before = null, CancellationToken cancellationToken = default)
        {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (conversationId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid conversation."
                });
            }

            if (contactId <= 0)
            {
                return BadRequest(new
                {
                    message = "Invalid contact."
                });
            }

            // Giới hạn để client không yêu cầu quá nhiều message
            limit = Math.Clamp(limit, 1, 50);

            var allowed = await _chatAuthorizationService.CanCustomerAccessConversationAsync(
                        conversationId, contactId, userId, guestToken, cancellationToken);

            if (!allowed)
            {
                return Forbid();
            }

            var result =
                await _chatMessageService.GetCustomerMessagesAsync(conversationId, limit, before, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{conversationId}/unread-count")]
        public async Task<IActionResult> GetUnreadCount(long conversationId, [FromHeader(Name = "X-Chat-Contact-Id")] long? contactId,
            [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _chatConversationService.GetUnreadCountAsync(conversationId, contactId, guestToken, cancellationToken);

            return Ok(result);
        }

        [HttpPost("{conversationId}/read")]
        public async Task<IActionResult> MarkAsRead(long conversationId, CancellationToken cancellationToken)
        {
            var contactIdHeader = Request.Headers["X-Chat-Contact-Id"].FirstOrDefault();
            var guestToken = Request.Headers["X-Chat-Guest-Token"].FirstOrDefault();

            if (!long.TryParse(contactIdHeader, out var contactId))
            {
                return Unauthorized();
            }
            var result = await _chatConversationService.MarkConversationAsReadAsync(conversationId,
                    contactId, guestToken, cancellationToken);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpGet("{conversationId:long}/previous")]
        public async Task<IActionResult> GetPreviousConversation(long conversationId, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var contactIdHeader = Request.Headers["X-Chat-Contact-Id"].FirstOrDefault();
            var guestToken = Request.Headers["X-Chat-Guest-Token"].FirstOrDefault();

            if (!long.TryParse(contactIdHeader, out var contactId))
            {
                return BadRequest(new
                {
                    message = "Invalid chat contact."
                });
            }

            var conversation = await _chatConversationService.GetPreviousResolvedConversationAsync(
                    conversationId, contactId, userId, guestToken, cancellationToken);

            if (conversation == null)
            {
                return NoContent();
            }

            return Ok(conversation);
        }



        [HttpPost("attachments/upload")]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment(
            [FromForm] IFormFile file,
            [FromHeader(Name = "X-Chat-Contact-Id")] long? contactId,
            [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
            [FromHeader(Name = "X-Chat-Upload-Token")] string? uploadSessionToken,
            CancellationToken cancellationToken)
        {
            if (file == null || file.Length <= 0)
            {
                return BadRequest(new
                {
                    message = "Vui lòng chọn file hợp lệ."
                });
            }

            var userId = User.Identity?.IsAuthenticated == true
                ? User.FindFirstValue(ClaimTypes.NameIdentifier)
                : null;

            var now = DateTime.UtcNow;
            long? guestSessionId = null;
            string? uploadTokenHash = null;
            string? issuedUploadSessionToken = null;

            bool isTemporaryGuestUpload = false;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                // User đăng nhập: quyền sở hữu dựa trên UserId.
                // Không nhận UserId từ client.
                if (!string.IsNullOrWhiteSpace(uploadSessionToken))
                {
                    return BadRequest(new
                    {
                        message = "Token upload tạm không áp dụng cho tài khoản đăng nhập."
                    });
                }
            }
            else
            {
                var hasContactId = contactId.HasValue && contactId.Value > 0;
                var hasGuestToken = !string.IsNullOrWhiteSpace(guestToken);

                // Chỉ chấp nhận một trong hai trạng thái:
                // 1. Guest đã có đủ contactId + guestToken.
                // 2. Guest mới chưa có cả hai.
                if (hasContactId != hasGuestToken ||
                    (contactId.HasValue && contactId.Value <= 0))
                {
                    return Unauthorized(new
                    {
                        message = "Phiên trò chuyện không hợp lệ."
                    });
                }

                if (hasContactId && hasGuestToken)
                {
                    // Guest đã có phiên: giữ nguyên cơ chế xác thực hiện tại.
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
                        return Unauthorized(new
                        {
                            message = "Phiên trò chuyện không hợp lệ."
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(uploadSessionToken))
                    {
                        return BadRequest(new
                        {
                            message = "Không cần token upload tạm cho phiên này."
                        });
                    }

                    guestSessionId = guestSession.Id;
                }
                else
                {
                    // Guest mới: chưa tạo contact hoặc guest session.
                    isTemporaryGuestUpload = true;

                    if (string.IsNullOrWhiteSpace(uploadSessionToken))
                    {
                        // Tạo token ngẫu nhiên có độ khó đoán cao.
                        issuedUploadSessionToken =
                            Convert.ToHexString(
                                System.Security.Cryptography.RandomNumberGenerator
                                    .GetBytes(32));

                        uploadTokenHash =
                            CommonHelper.Hash(issuedUploadSessionToken);
                    }
                    else
                    {
                        // Token đã có: chỉ chấp nhận nếu còn ít nhất một
                        // upload tạm đang Pending và chưa hết hạn.
                        uploadTokenHash =
                            CommonHelper.Hash(uploadSessionToken);

                        var hasActiveUpload = await _dbContext.ChatPendingUploads
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
                            return Unauthorized(new
                            {
                                message = "Phiên upload không hợp lệ hoặc đã hết hạn."
                            });
                        }
                    }
                }
            }

            MediaFile? mediaFile = null;
            var fileSavedToDisk = false;

            try
            {
                mediaFile = await _mediaService.UploadAsync(
                    file,
                    "chat",
                    cancellationToken);

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
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    uploadId = pendingUpload.Id,
                    fileName = mediaFile.OriginalFileName,
                    contentType = mediaFile.ContentType,
                    fileSize = mediaFile.Size,
                    expiresAt = pendingUpload.ExpiresAt,

                    // Chỉ trả token nguyên bản khi tạo nhóm upload tạm mới.
                    uploadSessionToken = issuedUploadSessionToken
                });
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                if (fileSavedToDisk && mediaFile != null)
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
                            "Không thể dọn file chat sau khi upload bị hủy.");
                    }
                }

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Không thể upload file đính kèm chat.");

                if (fileSavedToDisk && mediaFile != null)
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

                return StatusCode(500, new
                {
                    message = "Không thể tải file lên. Vui lòng thử lại."
                });
            }
        }


        [HttpGet("attachments/{attachmentId:long}/download")]
        public async Task<IActionResult> DownloadAttachment(long attachmentId, 
            [FromHeader(Name = "X-Chat-Contact-Id")] long? contactId, [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
            CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
            {
                return BadRequest(new
                {
                    message = "File đính kèm không hợp lệ."
                });
            }

            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Guest phải có đủ thông tin phiên.
            if (string.IsNullOrWhiteSpace(userId) &&
                (!contactId.HasValue ||
                 contactId.Value <= 0 ||
                 string.IsNullOrWhiteSpace(guestToken)))
            {
                return Unauthorized(new
                {
                    message = "Phiên trò chuyện không hợp lệ."
                });
            }

            var attachment = await _dbContext.ChatAttachments
                .AsNoTracking()
                .Include(x => x.Message)
                .Include(x => x.MediaFile)
                .FirstOrDefaultAsync(
                    x => x.Id == attachmentId,
                    cancellationToken);

            if (attachment == null || attachment.MediaFile == null || attachment.Message.IsPrivate)
            {
                return NotFound(new
                {
                    message = "Không tìm thấy file đính kèm."
                });
            }

            var conversationId = attachment.Message.ConversationId;

            var allowed =
                await _chatAuthorizationService.CanCustomerAccessConversationAsync(
                    conversationId,
                    contactId,
                    userId,
                    guestToken,
                    cancellationToken);

            if (!allowed)
            {
                // Dùng 404 để không tiết lộ file có tồn tại hay không
                // đối với người không có quyền truy cập.
                return NotFound(new
                {
                    message = "Không tìm thấy file đính kèm."
                });
            }

            FileStream? stream;

            try
            {
                stream = await _mediaService.OpenReadAsync(
                    attachment.MediaFile,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
            {
                _logger.LogError(
                    ex,
                    "Không thể đọc file chat attachment {AttachmentId}.",
                    attachmentId);

                return StatusCode(500, new
                {
                    message = "Không thể tải file. Vui lòng thử lại."
                });
            }

            if (stream == null)
            {
                return NotFound(new
                {
                    message = "File không còn tồn tại trên hệ thống."
                });
            }

            var fileName = Path.GetFileName(attachment.FileName.Replace('\\', '/'));

            if (string.IsNullOrWhiteSpace(fileName))
                fileName = $"attachment-{attachment.Id}";

            // Buộc tải xuống thay vì tin MIME type do client upload cung cấp.
            return File(
                stream,
                "application/octet-stream",
                fileName,
                enableRangeProcessing: true);
        }

        [HttpGet("attachments/{attachmentId:long}/preview")]
        public async Task<IActionResult> PreviewAttachment(
    long attachmentId,
    [FromHeader(Name = "X-Chat-Contact-Id")] long? contactId,
    [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
    CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
            {
                return BadRequest();
            }

            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Guest phải cung cấp đầy đủ danh tính.
            // Khách hàng đã đăng nhập được xác thực qua userId.
            if (string.IsNullOrWhiteSpace(userId) &&
                (!contactId.HasValue ||
                 contactId.Value <= 0 ||
                 string.IsNullOrWhiteSpace(guestToken)))
            {
                return Unauthorized();
            }

            var attachment = await _dbContext.ChatAttachments
                .AsNoTracking()
                .Include(x => x.Message)
                .Include(x => x.MediaFile)
                .FirstOrDefaultAsync(
                    x => x.Id == attachmentId,
                    cancellationToken);

            if (attachment?.Message == null ||
                attachment.MediaFile == null ||
                attachment.Message.IsPrivate)
            {
                return NotFound();
            }

            // Kiểm tra quyền xem hội thoại.
            var allowed =
                await _chatAuthorizationService.CanCustomerAccessConversationAsync(
                    attachment.Message.ConversationId,
                    contactId,
                    userId,
                    guestToken,
                    cancellationToken);

            if (!allowed)
            {
                return NotFound();
            }

            // Chỉ cho phép các định dạng ảnh raster đã được xác minh
            // từ nội dung file ở phía server khi upload.
            var contentType = attachment.MediaFile.ContentType?
                .Trim()
                .ToLowerInvariant();

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
                return NotFound();
            }

            FileStream? stream;

            try
            {
                stream = await _mediaService.OpenReadAsync(
                    attachment.MediaFile,
                    cancellationToken);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (
                ex is IOException or
                UnauthorizedAccessException or
                InvalidOperationException)
            {
                _logger.LogError(
                    ex,
                    "Failed to open attachment preview. AttachmentId: {AttachmentId}",
                    attachmentId);

                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            if (stream == null)
            {
                return NotFound();
            }

            // Không lưu cache ảnh riêng tư trong trình duyệt hoặc proxy.
            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Không truyền fileDownloadName để trình duyệt hiển thị ảnh
            // thay vì yêu cầu tải file xuống.
            return File(stream, contentType);
        }
    }
}