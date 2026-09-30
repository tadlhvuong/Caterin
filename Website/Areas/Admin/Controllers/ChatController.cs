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
        private readonly IChatMessageService _chatService;
        private readonly ICurrentUserService _currentUserService;

        public ChatController(
            IChatMessageService chatService, ICurrentUserService currentUserService)
        {
            _chatService = chatService;
            _currentUserService = currentUserService;
        }

        [HttpGet]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var inbox = await _chatService.GetDefaultInboxAsync(cancellationToken);

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
        public async Task<IActionResult> Conversations(
    long? inboxId,
    ChatConversationStatus? status,
    string? search, long? labelId,
    int limit = 30,
    DateTime? beforeLastMessageAt = null,
    long? beforeId = null,
    CancellationToken cancellationToken = default)
        {
            var result = await _chatService.GetConversationListAsync(
                inboxId,
                status,
                search,
                _currentUserService.UserId, labelId,
                limit,
                beforeLastMessageAt,
                beforeId,
                cancellationToken);

            return Ok(result);
        }

        [HttpGet("{conversationId:long}/contact")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Contact(long conversationId, CancellationToken cancellationToken)
        {
            var result = await _chatService.GetConversationContactAsync(conversationId, cancellationToken);

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
            var result = await _chatService.GetAdminMessagesAsync(
                conversationId,
                limit,
                before,
                cancellationToken);

            return Ok(result);
        }
        
        [HttpGet("conversations/counts")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Counts(
        long? inboxId,
        string? search = null,
        string? assignedUserId = null,
        long? labelId = null,
        CancellationToken cancellationToken = default)
            {
                var result = await _chatService.GetConversationCountsAsync(
                    inboxId,
                    search,
                    assignedUserId,
                    labelId,
                    cancellationToken);

                return Ok(result);
            }

        [HttpPost("labels")]
        [PermissionAction(ActionType.Create)]
        public async Task<IActionResult> CreateLabel(
        [FromBody] CreateChatLabelRequest request,
        CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu label không hợp lệ."
                });
            }

            try
            {
                var result = await _chatService.CreateLabelAsync(
                    request,
                    cancellationToken);

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
        public async Task<IActionResult> Labels(
    CancellationToken cancellationToken)
        {
            var result = await _chatService.GetLabelsAsync(
                cancellationToken);

            return Ok(result);
        }

        [HttpDelete("labels/{labelId:int}")]
        [PermissionAction(ActionType.Delete)]
        public async Task<IActionResult> DeleteLabel(
        int labelId,
        CancellationToken cancellationToken)
            {
                try
                {
                    var result =
                        await _chatService.DeleteLabelAsync(
                            labelId,
                            cancellationToken);

                    if (!result)
                    {
                        return NotFound(new
                        {
                            success = false,
                            message = "Không tìm thấy label."
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
        public async Task<IActionResult> AssignLabel(
    long conversationId,
    int labelId,
    CancellationToken cancellationToken)
        {
            try
            {
                var result = await _chatService.AssignConversationLabelAsync(
                    conversationId,
                    labelId,
                    cancellationToken);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy conversation."
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
        public async Task<IActionResult> RemoveLabel(
    long conversationId,
    int labelId,
    CancellationToken cancellationToken)
        {
            var result = await _chatService.RemoveConversationLabelAsync(
                conversationId,
                labelId,
                cancellationToken);

            if (!result)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Label chưa được gán cho conversation."
                });
            }

            return Ok(new
            {
                success = true
            });
        }
    }
}
