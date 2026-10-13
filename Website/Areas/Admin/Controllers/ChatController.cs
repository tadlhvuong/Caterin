using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Chat;
using Shared.Enums;
using Shared.Enums.Chat;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Chat;
using Shared.Interfaces.IdentityServices;
using Shared.Requests.Chat;
using Shared.Services.Chat;
using System.Reflection.Emit;
using System.Security.Claims;
using Website.Areas.Admin.Models;

namespace Website.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Route("admin/chat")]
    [PermissionModule("Chat")]
    public class ChatController : Controller
    {
        private readonly IChatAuthorizationService _chatAuthorizationService;
        private readonly IChatConversationService _chatConversationService;
        private readonly IChatMessageService _chatMessageService;
        private readonly IChatAttachmentService _chatAttachmentService;

        private readonly IChatLabelService _chatLabelService;
        private readonly ICurrentUserService _currentUserService;

        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatAuthorizationService chatAuthorizationService, IChatConversationService chatConversationService, 
            IChatMessageService chatMessageService, IChatAttachmentService chatAttachmentService,
            IChatLabelService chatLabelService, ICurrentUserService currentUserService,
            ILogger<ChatController> logger)
        {
            _chatAuthorizationService = chatAuthorizationService;
            _chatConversationService = chatConversationService;
            _chatMessageService = chatMessageService;
            _chatAttachmentService = chatAttachmentService;
            _chatLabelService = chatLabelService;
            _currentUserService = currentUserService;

            _logger = logger;
        }

        [HttpGet]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var inbox = await _chatConversationService.GetDefaultInboxAsync(cancellationToken);

            if (inbox == null)
            {
                return NotFound();
            }

            var model = new ChatWorkspaceViewModel
            {
                InboxId = inbox.Id
            };

            return View(model);
        }

        [HttpGet("conversations")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Conversations(long? inboxId, ChatConversationStatus? status, string? search, long? labelId,
            int limit = 30, DateTime? beforeLastMessageAt = null, long? beforeId = null, CancellationToken cancellationToken = default)
        {
            var result = await _chatConversationService.GetConversationListAsync(inboxId, status, search, _currentUserService.UserId, labelId,
                limit, beforeLastMessageAt, beforeId, cancellationToken);

            return Ok(result);
        }

        [HttpGet("{conversationId:long}/header")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Header(
    long conversationId,
    CancellationToken cancellationToken)
        {
            var result = await _chatConversationService
                .GetConversationHeaderAsync(
                    conversationId,
                    cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("{conversationId:long}/contact")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Contact(long conversationId, CancellationToken cancellationToken)
        {
            var result = await _chatConversationService.GetConversationContactAsync(conversationId, cancellationToken);

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        [HttpGet("{conversationId:long}/messages")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Messages(long conversationId, int limit = 30, long? before = null, CancellationToken cancellationToken = default)
        {
            var result = await _chatMessageService.GetAdminMessagesAsync(conversationId, limit, before, cancellationToken);

            return Ok(result);
        }
        
        [HttpGet("conversations/counts")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Counts(long? inboxId, string? search = null, string? assignedUserId = null, long? labelId = null,
            CancellationToken cancellationToken = default)
            {
                var result = await _chatConversationService.GetConversationCountsAsync(inboxId, search, assignedUserId, labelId, cancellationToken);

                return Ok(result);
            }

        [HttpPost("labels")]
        [PermissionAction(ActionType.Create)]
        public async Task<IActionResult> CreateLabel([FromBody] CreateChatLabelRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid label data."
                });
            }

            try
            {
                var result = await _chatLabelService.CreateLabelAsync(request, cancellationToken);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("labels")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Labels(CancellationToken cancellationToken)
        {
            var result = await _chatLabelService.GetLabelsAsync(cancellationToken);

            return Ok(result);
        }

        [HttpDelete("labels/{labelId:int}")]
        [PermissionAction(ActionType.Delete)]
        public async Task<IActionResult> DeleteLabel(int labelId, CancellationToken cancellationToken)
            {
                try
                {
                    var result = await _chatLabelService.DeleteLabelAsync(labelId, cancellationToken);

                    if (!result)
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "Label not found."
                        });
                    }

                    return Ok(new
                    {
                        success = true
                    });
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = ex.Message
                    });
                }
            }

        [HttpPost("{conversationId:long}/labels/{labelId:int}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> AssignLabel(long conversationId, int labelId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _chatLabelService.AssignConversationLabelAsync(conversationId, labelId, cancellationToken);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Conversation not found."
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{conversationId:long}/labels/{labelId:int}")]
        [PermissionAction(ActionType.Edit)]
        public async Task<IActionResult> RemoveLabel(long conversationId, int labelId, CancellationToken cancellationToken)
        {
            var result = await _chatLabelService.RemoveConversationLabelAsync(conversationId, labelId, cancellationToken);

            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Label is not assigned to the conversation."
                });
            }

            return Ok(new
            {
                success = true
            });
        }

        [HttpPost("attachments/upload")]
        [PermissionAction(ActionType.View)]
        [RequestSizeLimit(12 * 1024 * 1024)]
        public async Task<IActionResult> UploadAttachment(
    [FromForm] IFormFile file,
    CancellationToken cancellationToken)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized();

            try
            {
                var result = await _chatAttachmentService.UploadAsync(
                    file,
                    contactId: null,
                    guestToken: null,
                    uploadSessionToken: null,
                    userId: userId,
                    cancellationToken: cancellationToken);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new
                {
                    message = "Không thể xác thực phiên upload."
                });
            }
        }

        [HttpGet("attachments/{attachmentId:long}/download")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> DownloadAttachment(
    long attachmentId,
    CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
                return BadRequest(new { message = "File đính kèm không hợp lệ." });

            var result = await _chatAttachmentService.OpenForAdminDownloadAsync(
                attachmentId,
                cancellationToken);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy file đính kèm." });
            Response.Headers["Cache-Control"] = "private, no-store";
            return File(
                result.Stream,
                result.ContentType,
                result.FileName,
                enableRangeProcessing: true);
        }

        [HttpGet("attachments/{attachmentId:long}/preview")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> PreviewAttachment(
            long attachmentId,
            CancellationToken cancellationToken)
        {
            if (attachmentId <= 0)
                return BadRequest(new { message = "File đính kèm không hợp lệ." });

            var result = await _chatAttachmentService.OpenForAdminPreviewAsync(
                attachmentId,
                cancellationToken);

            if (result == null)
                return NotFound(new { message = "Không tìm thấy file đính kèm." });

            Response.Headers["Cache-Control"] = "private, no-store";

            return File(result.Stream, result.ContentType);
        }
    }
}
