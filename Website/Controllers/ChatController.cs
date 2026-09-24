using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;
using Shared.Interfaces.Chat;
using Shared.Requests.Chat;
using System.Security.Claims;
using System.Xml.Linq;

namespace Website.Controllers
{
    [Route("chat")]
    public class ChatController : Controller
    {

        private readonly AppDbContext _dbContext;
        private readonly IChatMessageService _chatMessageService;

        private readonly ILogger _logger;
        public ChatController(AppDbContext dbContext, IChatMessageService chatMessageService,
            ILogger<ChatController> logger)
        {
            _dbContext = dbContext;
            _chatMessageService = chatMessageService;

            _logger = logger;
        }

        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] StartChatRequest request, 
            CancellationToken cancellationToken)
        {
            var userId = User.Identity?.IsAuthenticated == true ? User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    //        var inbox = await _dbContext.ChatInboxes
    //.FirstOrDefaultAsync(
    //    x => x.ChannelType == Shared.Enums.Chat.ChatChannelType.Website &&
    //         x.IsActive,
    //    cancellationToken);
            //request.InboxId = inbox.Id;
            var conversation =
                await _chatMessageService.StartConversationAsync(request, userId, cancellationToken);

            return Ok(conversation);
        }

        [HttpGet("{conversationId:long}/messages")]
        public async Task<IActionResult> GetConversationMessages(
    long conversationId,
    [FromHeader(Name = "X-Chat-Contact-Id")] long contactId,
    [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
    [FromQuery] int limit = 30,
    [FromQuery] long? before = null,
    CancellationToken cancellationToken = default)
        {
            var userId = User?
                .FindFirst(ClaimTypes.NameIdentifier)?
                .Value;

            if (conversationId <= 0)
            {
                return BadRequest(new
                {
                    message = "Conversation không hợp lệ."
                });
            }

            if (contactId <= 0)
            {
                return BadRequest(new
                {
                    message = "Contact không hợp lệ."
                });
            }

            // Giới hạn để client không yêu cầu quá nhiều message
            limit = Math.Clamp(limit, 1, 50);

            var allowed =
                await _chatMessageService
                    .CanCustomerAccessConversationAsync(
                        conversationId,
                        contactId,
                        userId,
                        guestToken,
                        cancellationToken);

            if (!allowed)
            {
                return Forbid();
            }

            var result =
                await _chatMessageService
                    .GetCustomerMessagesAsync(
                        conversationId,
                        limit,
                        before,
                        cancellationToken);

            return Ok(result);
        }

        [HttpGet("{conversationId}/unread-count")]
        public async Task<IActionResult> GetUnreadCount(
    long conversationId,
    [FromHeader(Name = "X-Chat-Contact-Id")] long? contactId,
    [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
    CancellationToken cancellationToken)
        {
            var result = await _chatMessageService.GetUnreadCountAsync(
                conversationId,
                contactId,
                guestToken,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{conversationId}/read")]
        public async Task<IActionResult> MarkAsRead(
    long conversationId,
    CancellationToken cancellationToken)
        {
            var contactIdHeader = Request.Headers["X-Chat-Contact-Id"]
                .FirstOrDefault();

            var guestToken = Request.Headers["X-Chat-Guest-Token"]
                .FirstOrDefault();
            if (!long.TryParse(contactIdHeader, out var contactId))
            {
                return Unauthorized();
            }
            var result =
                await _chatMessageService.MarkConversationAsReadAsync(
                    conversationId,
                    contactId,
                    guestToken,
                    cancellationToken);

            if (!result.Succeeded)
                return BadRequest(result);

            return Ok(result);
        }
    }
}