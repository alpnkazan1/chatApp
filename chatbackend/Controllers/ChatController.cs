using System.Security.Claims;
using chatbackend.Data;
using chatbackend.DTOs.Chats;
using chatbackend.Models;
using chatbackend.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace chatbackend.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDBContext _context;        
        private readonly MyAuthorizationService _authCheckService;


        public ChatController(ApplicationDBContext context, MyAuthorizationService authCheckService)
        {
            _context = context;
            _authCheckService = authCheckService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequestDto chatDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if the chat already exists
            var existingChat = await _context.Chats
                .FirstOrDefaultAsync(c => 
                    (c.User1Id.ToString() == chatDto.User1Id && c.User2Id.ToString() == chatDto.User2Id) ||
                    (c.User1Id.ToString() == chatDto.User2Id && c.User2Id.ToString() == chatDto.User1Id));

            if (existingChat != null)
            {
                return Conflict(new { message = "Chat already exists.", chatId = existingChat.ChatId });
            }
            
            // Initialize user variables
            User user1 = null;
            User user2 = null;

            // Fetch User1 from the database
            if (Guid.TryParse(chatDto.User1Id, out Guid user1Id))
            {
                user1 = await _context.Users.FindAsync(user1Id);
            }
            else
            {
                return BadRequest(new { message = "Invalid GUID format for User1Id." });
            }

            // Fetch User2 from the database
            if (Guid.TryParse(chatDto.User2Id, out Guid user2Id))
            {
                user2 = await _context.Users.FindAsync(user2Id);
            }
            else
            {
                return BadRequest(new { message = "Invalid GUID format for User2Id." });
            }

            // Check if users are found
            if (user1 == null || user2 == null)
            {
                return NotFound(new { message = "One or both users not found." });
            }

            // Create a new chat with User navigation properties
            var newChat = new Chat
            {
                ChatId = Guid.NewGuid(),
                User1Id = user1.Id,
                User2Id = user2.Id,
                User1 = user1,
                User2 = user2,
                UserName1 = user1.UserName,
                UserName2 = user2.UserName,
                LastMessageTime = null
            };

            await _context.Chats.AddAsync(newChat);
            await _context.SaveChangesAsync();

            var response = new ChatCreatedDto
            {
                ChatId = newChat.ChatId.ToString(),
                User1Id = newChat.User1Id.ToString(),
                User2Id = newChat.User2Id.ToString()
            };

            return Ok(response);
        }

    
        [HttpGet("photos/{chatId}")]
        public async Task<IActionResult> GetChatPhotos(Guid chatId, [FromQuery] int limit = 10, [FromQuery] int offset = 0)
        {
            // Check if the user is authorized for this chat
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isAuthorized = await _authCheckService.IsAuthorizedForChat(userId, chatId);

            if (!isAuthorized)
            {
                return Forbid(); // Or Unauthorized, depending on your policy
            }

            // Fetch images from this chat which belongs to this user
            // with the necessary access clearance
            var photos = await _context.ACL
                .Where(a => 
                    a.FolderName == "images" && 
                    a.ChatId == chatId &&
                    a.UserId == Guid.Parse(userId) &&
                    (a.AccessType == AccessType.Read || a.AccessType == AccessType.Full)
                )
                .OrderByDescending(a => a.UpdateTime) // Order by ACL UpdateTime
                .Skip(offset)
                .Take(limit)
                .Select(a => new
                {
                    a.FileId,
                    a.FolderName,
                    a.FileExtension
                })
                .ToListAsync();

            if (!photos.Any())
            {
                return NotFound("No photos found for this chat.");
            }

            // Generate secure URLs for photos
            var photoUrls = photos.Select(p => new
            {
                FileId = p.FileId,
                FileUrl = _authCheckService.GenerateSecuredFileURL(
                                p.FolderName, 
                                p.FileId.ToString() + p.FileExtension
                            )
            }).ToList();

            return Ok(photoUrls);
        }
    
    }
}