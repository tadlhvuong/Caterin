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
        private readonly IChatAttachmentService _chatAttachmentService;
        private readonly IMediaService _mediaService;

        private readonly ILogger _logger;
        public ChatController(AppDbContext dbContext, IChatAuthorizationService chatAuthorizationService,
            IChatConversationService chatConversationService, IChatMessageService chatMessageService,
            IChatAttachmentService chatAttactmentService, IMediaService mediaService,
            ILogger<ChatController> logger)
        {
            _dbContext = dbContext;

            _chatAuthorizationService = chatAuthorizationService;
            _chatConversationService = chatConversationService;
            _chatMessageService = chatMessageService;
            _chatAttachmentService = chatAttactmentService;
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

            var result = await _chatAttachmentService.UploadAsync(
                file,
                contactId,
                guestToken,
                uploadSessionToken,
                userId,
                cancellationToken);

            return Ok(new
            {
                uploadId = result.UploadId,
                fileName = result.FileName,
                contentType = result.ContentType,
                fileSize = result.FileSize,
                expiresAt = result.ExpiresAt,
                uploadSessionToken = result.UploadSessionToken
            });
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

            var result = await _chatAttachmentService.OpenForDownloadAsync(attachmentId, contactId, guestToken, userId, cancellationToken);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy file đính kèm." });

            return File(
                result.Stream,
                result.ContentType,
                result.FileName,
                enableRangeProcessing: true);
        }

        [HttpGet("attachments/{attachmentId:long}/preview")]
        public async Task<IActionResult> PreviewAttachment(long attachmentId, [FromHeader(Name = "X-Chat-Contact-Id")] long? contactId,
            [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken, CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
            {
                return BadRequest();
            }

            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Guest phải cung cấp đầy đủ danh tính.
            // Khách hàng đã đăng nhập được xác thực qua userId.
            if (string.IsNullOrWhiteSpace(userId) && (!contactId.HasValue || contactId.Value <= 0 || string.IsNullOrWhiteSpace(guestToken)))
            {
                return Unauthorized();
            }

            var result = await _chatAttachmentService.OpenForPreviewAsync(attachmentId, contactId, guestToken, userId, cancellationToken);

            if (result == null)
                return NotFound();

            Response.Headers["Cache-Control"] = "private, no-store";

            return File(result.Stream, result.ContentType);
        }
    }
}