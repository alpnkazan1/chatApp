using System.Security.Claims;
using chatbackend.Data;
using chatbackend.DTOs.Users;
using chatbackend.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace chatbackend.Controllers
{
    [Route("api/users")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly MyAuthorizationService _authorizationCheckService;

        public UserController(ApplicationDBContext context, MyAuthorizationService authorizationCheckService)
        {
            _context = context;
            _authorizationCheckService = authorizationCheckService;
        }

        [HttpGet("search")]
        [Authorize]
        public async Task<IActionResult> SearchUser([FromQuery] string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return BadRequest("Username is required");
            }

            var user = await _context.Users
                .Where(u => u.UserName.ToLower() == username.ToLower()) // I am not sure about making users case insensitive
                .FirstOrDefaultAsync();
            if (user == null)
            {
                return NotFound("User not found");
            }

            //Generate and store the avatar URL
            string? avatarUrl = null;
            if (user.AvatarId.HasValue) // Check if AvatarId has a value (is not null)
            {
                avatarUrl = _authorizationCheckService.GenerateSecuredAvatarURL(user.AvatarId.Value.ToString());
            }
            
            var userResponse = new UserSearchDto
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                AvatarUrl = avatarUrl
            };

            return Ok(userResponse);
        }
    
        [HttpPost("block-user/{userName}")]
        [Authorize]
        public async Task<IActionResult> BlockUser(string userName)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User is not authenticated");
            }

            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == userName);
            if (targetUser == null)
            {
                return NotFound("User not found");
            }

            var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                (c.User1Id.ToString() == userId && c.User2Id == targetUser.Id) ||
                (c.User2Id.ToString() == userId && c.User1Id == targetUser.Id));

            if (chat == null)
            {
                return NotFound("No chat exists between you and this user");
            }

            // Determine blocking logic
            if (chat.User1Id.ToString() == userId)
            {
                chat.BlockFlag = (uint)(chat.BlockFlag == 2 ? 3 : 1); // If User2 blocked (2), set 3. Otherwise, set 1.
            }
            else if (chat.User2Id.ToString() == userId)
            {
                chat.BlockFlag = (uint)(chat.BlockFlag == 1 ? 3 : 2); // If User1 blocked (1), set 3. Otherwise, set 2.
            }
            else
            {
                return Forbid("You are not part of this chat");
            }

            await _context.SaveChangesAsync();

            return Ok("User blocked successfully");
        }

    }
}