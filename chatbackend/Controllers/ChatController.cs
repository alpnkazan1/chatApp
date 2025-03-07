using chatbackend.Data;
using chatbackend.DTOs.Chats;
using chatbackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace chatbackend.Controllers
{
    [ApiController]
    [Route("api/chats")]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public ChatController(ApplicationDBContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateChat([FromBody] CreateChatRequestDto chatDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if the chat already exists
            var existingChat = await _context.Chats
                .FirstOrDefaultAsync(c => 
                    (c.User1Id == chatDto.User1Id && c.User2Id == chatDto.User2Id) ||
                    (c.User1Id == chatDto.User2Id && c.User2Id == chatDto.User1Id));

            if (existingChat != null)
            {
                return Conflict(new { message = "Chat already exists.", chatId = existingChat.ChatId });
            }
            // Fetch User1 and User2 from the database
            var user1 = await _context.Users.FindAsync(chatDto.User1Id);
            var user2 = await _context.Users.FindAsync(chatDto.User2Id);

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
                ChatId = newChat.ChatId,
                User1Id = newChat.User1Id,
                User2Id = newChat.User2Id
            };

            return Ok(response);
        }
    }


}