using Microsoft.AspNetCore.Mvc;
using chatbackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using chatbackend.Repository;
using chatbackend.Data;
using chatbackend.DTOs.Messages;
using chatbackend.Service;


namespace chatbackend.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessageController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<MessageController> _logger;
        private readonly FileSystemAccess _fileSystemAccess;
        private readonly MyAuthorizationService _authCheckService;

        public MessageController(ApplicationDBContext context, ILogger<MessageController> logger, 
                                FileSystemAccess fileSystemAccess, MyAuthorizationService authCheckService)
        {
            _context = context;
            _logger = logger;
            _fileSystemAccess = fileSystemAccess;
            _authCheckService = authCheckService;
        }

        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetMessagesByChatId(Guid chatId, [FromQuery] int? limit)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);


            bool isAuthorized = await _authCheckService.IsAuthorizedForChat(userId, chatId);

            if (!isAuthorized)
            {
                return Forbid(); // Or Unauthorized, depending on your policy
            }

            // If a limit is given take minimum of 50 and the given limit else take 50
            int messageLimit = (limit.HasValue && limit.Value > 0) ? Math.Min(limit.Value, 50) : 50;

            // Get 50 messages by their chat Id
            var messages = await _context.Messages
                .Where(m => m.ChatId == chatId)
                .OrderByDescending(m => m.Timestamp) // Order by timestamp (or appropriate field)
                .Take(messageLimit) // Take messageLimit number of messages
                .ToListAsync();

            if (messages == null || messages.Count == 0)
                return NotFound("No messages found with this chat id.");

            //Check authorization and construct secured urls to serve from frontend.
            var messageResponses = new List<MessageReqDto>();
            foreach (var message in messages)
            {
                try
                {
                    string fileUrl = null; // Default to null for no file

                    if (message.FileFlag != 0)
                    {
                        string folderName = _fileSystemAccess.GetSubfolder((uint)message.FileFlag);
                        string fileNameWithExtension = message.FileId.ToString() + "." + message.FileExtension;
                        fileUrl = _authCheckService.GenerateSecuredFileURL(folderName, fileNameWithExtension);
                    }
                    
                    var messageResponseDto = new MessageReqDto
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


        [HttpGet("messageIdCheck/{messageId}")]
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
            bool isAuthorized = await _authCheckService.IsAuthorizedForChat(userId, message.ChatId);

            if (!isAuthorized)
            {
                return Forbid(); // Or Unauthorized, depending on your policy
            }

            string fileUrl = null; // Default to null for no file
            
            if (message.FileFlag != 0)
            {
                string folderName = _fileSystemAccess.GetSubfolder((uint)message.FileFlag);
                string fileNameWithExtension = message.FileId.ToString() + "." + message.FileExtension;
                fileUrl = _authCheckService.GenerateSecuredFileURL(folderName, fileNameWithExtension);
            }

            try
            {
                var messageResponseDto = new MessageReqDto
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