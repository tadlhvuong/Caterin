using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Data.Entities.Identity;
using Shared.DTOs.Chat;
using Shared.Enums;
using Shared.Interfaces.AuthServices;
using Shared.Interfaces.Chat;
using Shared.Interfaces.IdentityServices;
using Shared.Requests.Chat;
using Shared.Services.Chat;
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
    int limit = 30,
    DateTime? beforeLastMessageAt = null,
    long? beforeId = null,
    CancellationToken cancellationToken = default)
        {
            var result = await _chatService.GetConversationListAsync(
                inboxId,
                _currentUserService.UserId,
                limit,
                beforeLastMessageAt,
                beforeId,
                cancellationToken);

            return Ok(result);
        }

        //    [HttpGet("conversation/{id:long}")]
        //    [PermissionAction(ActionType.View)]
        //    public async Task<IActionResult> Conversation(
        //long id,
        //CancellationToken cancellationToken)
        //    {
        //        var result = await _chatService
        //            .GetConversationDetailAsync(
        //                id,
        //                cancellationToken);

        //        if (result == null)
        //            return NotFound();

        //        return PartialView(
        //            "_ConversationDetail",
        //            result);
        //    }


        [HttpGet("contact")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Contact(
            long conversationId,
            CancellationToken cancellationToken)
        {
            var result = await _chatService
                .GetConversationContactAsync(
                    conversationId,
                    cancellationToken);

            if (result == null)
                return NotFound();

            return PartialView(
                "_ContactDrawer",
                result);
        }
        [HttpGet("{conversationId:long}/messages")]
        [PermissionAction(ActionType.View)]
        public async Task<IActionResult> Messages(
    long conversationId,
    int limit = 30,
    long? before = null,
    CancellationToken cancellationToken = default)
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
    }
}
