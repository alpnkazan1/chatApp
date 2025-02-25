using Microsoft.AspNetCore.Mvc;
using chatbackend.Helpers;
using chatbackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using chatbackend.Data;
using chatbackend.DTOs.Messages;
using chatbackend.Repository;

namespace chatbackend.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly IUrlHelper _urlHelper;
        private readonly ILogger<MessageController> _logger;

        public MessageController(chatbackend.Data.ApplicationDBContext context, IUrlHelper urlHelper, ILogger<MessageController> logger)
        {
            _context = context;
            _urlHelper = urlHelper;
            _logger = logger;
        }

        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetMessagesByChatId(Guid chatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            bool isAuthorized = await IsAuthorizedForChat(_context, userId, chatId);

            if (!isAuthorized)
            {
                return Forbid(); // Or Unauthorized, depending on your policy
            }

            // Get 50 messages by their chat Id
            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.Timestamp) // Order by timestamp (or appropriate field)
                .Take(50) // Take only the last 50 messages
                .ToListAsync();

            if (messages == null || messages.Count == 0)
                return NotFound("No messages found with this chat id.");

            //Check authorization and construct secured urls to serve from frontend.
            var messageResponses = new List<object>();
            foreach (var message in messages)
            {
                try
                {
                    string fileUrl = GenerateSecuredFileURL(message, _urlHelper);
                    
                    var messageResponseDto = new MessageResponseDto
                    {
                        MessageId = message.MessageId,
                        ChatId = message.ChatId,
                        SenderId = message.SenderId,
                        ReceiverId = message.ReceiverId,
                        MessageText = message.MessageText,
                        FileFlag = message.FileFlag,
                        FileId = message.FileId,
                        FileExtension = message.FileExtension,
                        Timestamp = message.Timestamp,
                        FileUrl = fileUrl // Add secured file URL
                    };
                    messageResponses.Add(messageResponseDto);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
                    return StatusCode(500, $"Error processing message {message.MessageId}");
                }
            }

            return Ok(messageResponses);
        }

        public async Task<IActionResult> GetMessageById(Guid messageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Get the message by Id
            var message = await _context.Messages
                .Where(m => m.MessageId == messageId)
                .FirstOrDefaultAsync();

            if (message == null)
            {
                return NotFound("No messages found with this id.");
            }
            bool isAuthorized = await IsAuthorizedForChat(_context, userId, message.ChatId);

            if (!isAuthorized)
            {
                return Forbid(); // Or Unauthorized, depending on your policy
            }

            try
            {
                string fileUrl = GenerateSecuredFileURL(message, _urlHelper);

                var messageResponseDto = new MessageResponseDto
                {
                    MessageId = message.MessageId,
                    ChatId = message.ChatId,
                    SenderId = message.SenderId,
                    ReceiverId = message.ReceiverId,
                    MessageText = message.MessageText,
                    FileFlag = message.FileFlag,
                    FileId = message.FileId,
                    FileExtension = message.FileExtension,
                    Timestamp = message.Timestamp,
                    FileUrl = fileUrl
                };

                return Ok(messageResponseDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}", message.MessageId);
                return StatusCode(500, $"Error processing message {message.MessageId}");
            }
        }
    }
}