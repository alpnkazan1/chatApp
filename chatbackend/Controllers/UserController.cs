using chatbackend.Data;
using chatbackend.DTOs.Users;
using chatbackend.Service;
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
    }
}