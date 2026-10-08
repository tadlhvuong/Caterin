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

        private readonly IChatAuthorizationService _chatAuthorizationService;
        private readonly IChatConversationService _chatConversationService;
        private readonly IChatMessageService _chatMessageService;

        private readonly ILogger _logger;
        public ChatController(AppDbContext dbContext, IChatAuthorizationService chatAuthorizationService,
            IChatConversationService chatConversationService, IChatMessageService chatMessageService, 
            ILogger<ChatController> logger)
        {
            _dbContext = dbContext;

            _chatAuthorizationService = chatAuthorizationService;
            _chatConversationService = chatConversationService;
            _chatMessageService = chatMessageService;

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
    }
}