using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using chatbackend.Data;
using chatbackend.DTOs.Account;
using chatbackend.Interfaces;
using chatbackend.Models;
using chatbackend.Repository;
using chatbackend.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace chatbackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly FileSystemAccess _fileSystemAccess;
        private readonly ApplicationDBContext _context;
        private readonly MyAuthorizationService _authorizationCheckService;

        public AccountController(UserManager<User> userManager, 
                                ITokenService tokenService, 
                                SignInManager<User> signInManager,
                                ILogger<AccountController> logger,
                                FileSystemAccess fileSystemAccess,
                                ApplicationDBContext context,
                                MyAuthorizationService authorizationCheckService)
        {
            _fileSystemAccess = fileSystemAccess;
            _context = context;
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _logger = logger;
            _authorizationCheckService = authorizationCheckService;
        }

        [HttpPost("login")]      
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login attempt for email {Email}.  Model state errors: {ModelState}", loginDto.Email, ModelState);
                return BadRequest(ModelState);
            }
                
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Email == loginDto.Email);
            
            if (user == null)
            {
                _logger.LogWarning("Login attempt for non-existent email: {Email}", loginDto.Email);
                return Unauthorized("Invalid username!");
            }

            // Supposedly lockoutonfailure is a problematic aspect and should not be utilized
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);


            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt for user {UserName} from IP address {IPAddress}.", user.UserName, HttpContext.Connection.RemoteIpAddress);
                return Unauthorized("Issue with username and/or password!");
            }

            // Generate the tokens for access and refresh
            var accessToken = _tokenService.CreateToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Update User entity with new refresh token and record it to db
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);
            await _userManager.UpdateAsync(user);

            return Ok(
                new LogResponseDto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Token = accessToken,
                    RefreshToken = refreshToken
                }
            );
        }     

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] RegisterDto registerDto)  
        {
            try
            {
                var validationResult = InputValidation.ValidateUsername(registerDto.Username, _logger);
                if (validationResult != null)
                {
                    return validationResult; // Return the BadRequest result
                }

                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid registration attempt. Model state errors: {ModelStateErrors}", ModelState);
                    return BadRequest(ModelState);
                }

                var appUser = new User
                {
                    UserName = registerDto.Username,
                    Email = registerDto.Email
                };

                var createdUserResult = await _userManager.CreateAsync(appUser, registerDto.Password);

                if (!createdUserResult.Succeeded)
                {
                    _logger.LogError("Failed to create user {Email}. Errors: {Errors}", appUser.Email, createdUserResult.Errors);
                    return StatusCode(500, createdUserResult.Errors);
                }

                _logger.LogInformation("User {Username} created successfully.", appUser.UserName);

                var roleResult = await _userManager.AddToRoleAsync(appUser, "ChatUser");

                if (!roleResult.Succeeded)
                {
                    _logger.LogError("Failed to add user {Username} to ChatUser role. Errors: {Errors}", appUser.UserName, roleResult.Errors);
                    return StatusCode(500, roleResult.Errors);
                }

                _logger.LogInformation("User {Username} added to ChatUser role.", appUser.UserName);

                if (registerDto.Avatar != null)
                {
                    Guid avatarId = Guid.NewGuid();
                    bool uploadSuccess = await _fileSystemAccess.UploadFileAsync(registerDto.Avatar, 4, avatarId);

                    if (!uploadSuccess)
                    {
                        _logger.LogError("Failed to upload avatar for user {Username}", appUser.UserName);
                        return StatusCode(500, "Failed to upload avatar.");
                    }

                    appUser.AvatarId = avatarId;
                    var updateResult = await _userManager.UpdateAsync(appUser);

                    if (!updateResult.Succeeded)
                    {
                        _logger.LogError("Failed to update user {Username} with avatar URL. Errors: {Errors}", appUser.UserName, updateResult.Errors);
                        return StatusCode(500, "Failed to update user with avatar URL.");
                    }
                }

                // Generate tokens
                var accessToken = _tokenService.CreateToken(appUser);
                var refreshToken = _tokenService.GenerateRefreshToken();

                // Add refresh token to database
                appUser.RefreshToken = refreshToken;
                appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);

                var finalUpdateResult = await _userManager.UpdateAsync(appUser);

                if (!finalUpdateResult.Succeeded)
                {
                    _logger.LogError("Failed to update user {Username} with refresh token. Errors: {Errors}", appUser.UserName, finalUpdateResult.Errors);
                    return StatusCode(500, "Failed to update user with refresh token.");
                }

                return Ok(new LogResponseDto
                {
                    UserName = appUser.UserName,
                    Email = appUser.Email,
                    Token = accessToken,
                    RefreshToken = refreshToken
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Exception during registration.");
                return StatusCode(500, e);
            }
        }

    
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            // Get the currently logged-in user
            var user = await _userManager.GetUserAsync(User); // Get user from ClaimsPrincipal

            if (user == null)
            {
                _logger.LogWarning("Logout attempt for non-existent user.");
                return BadRequest("User not found.");
            }

            // Revoke the refresh token (if one exists)
            if (!string.IsNullOrEmpty(user.RefreshToken))
            {
                try
                {
                    await _tokenService.RevokeTokenAsync(user.RefreshToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error revoking refresh token for user {UserId}.", user.Id);
                    return StatusCode(500, "Error revoking refresh token.");
                }
            }
            else
            {
                _logger.LogInformation("No refresh token found for user {UserId} during logout.", user.Id);
            }

            // Sign out the user (clears authentication cookies, etc.)
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User {UserId} logged out successfully.", user.Id);

            return Ok("Logged out successfully.");
        }

        [HttpGet("check")]
        [Authorize] // Requires authentication
        public async Task<IActionResult> CheckAuthentication()
        {
            // Get the user ID from the authenticated user's claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User is authenticated but NameIdentifier claim is missing.");
                return Unauthorized(); // Or a more specific error message
            }

            // Fetch user information from the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user == null)
            {
                _logger.LogWarning($"User with ID {userId} not found in the database.");
                return NotFound("User not found");
            }

            //Generate avatar and store it as URL
            string? avatarUrl = null;
            if (user.AvatarId.HasValue) // Check if AvatarId has a value (is not null)
            {
                avatarUrl = _authorizationCheckService.GenerateSecuredAvatarURL(user.AvatarId.Value.ToString());
            }
            // Construct the response object
            var response = new AuthCheckDto
            {
                Id = user.Id.ToString(),
                UserName = user.UserName,
                Email = user.Email,
                Avatar = avatarUrl // Server should return a URL for avatar
            };

            return Ok(response);
        }
    
        
    }
}