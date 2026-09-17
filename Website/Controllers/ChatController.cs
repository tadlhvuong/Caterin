using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Constants.Permission;
using Shared.Data.Context;
using Shared.Enums;
using Shared.Interfaces.Chat;
using Shared.Requests.Chat;
using System.Security.Claims;

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

        [HttpGet("conversations/{conversationId:long}/messages")]
        public async Task<IActionResult> GetConversationMessages(
        long conversationId,
        [FromHeader(Name = "X-Chat-Contact-Id")] long contactId,
        [FromHeader(Name = "X-Chat-Guest-Token")] string? guestToken,
        CancellationToken cancellationToken)
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

            var messages =
                await _chatMessageService
                    .GetCustomerMessagesAsync(
                        conversationId,
                        cancellationToken);

            return Ok(messages);
        }
    }
}