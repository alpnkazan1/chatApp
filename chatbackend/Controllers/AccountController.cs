using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.DTOs.Account;
using chatbackend.Interfaces;
using chatbackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace chatbackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AuthController> _logger;
        public AuthController(UserManager<User> userManager, 
                                ITokenService tokenService, 
                                SignInManager<User> signInManager,
                                ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _logger = logger;
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
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
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

                if(createdUserResult.Succeeded)
                {
                    _logger.LogInformation("User {Username} created successfully.", appUser.UserName);

                    var roleResult = await _userManager.AddToRoleAsync(appUser, "ChatUser");

                    if(roleResult.Succeeded)
                    {
                        _logger.LogInformation("User {Username} added to ChatUser role.", appUser.UserName);
                        // Generate the tokens
                        var accessToken = _tokenService.CreateToken(appUser);
                        var refreshToken = _tokenService.GenerateRefreshToken();
                        // Add refresh token to database
                        appUser.RefreshToken = refreshToken;
                        appUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);

                        var updateResult = await _userManager.UpdateAsync(appUser);

                        if (!updateResult.Succeeded)
                        {
                            _logger.LogError("Failed to update user {Username} with refresh token. Errors: {Errors}", appUser.UserName, updateResult.Errors);
                            return StatusCode(500, "Failed to update user with refresh token.");
                        }

                        return Ok(
                            new LogResponseDto
                            {
                                UserName = appUser.UserName,
                                Email = appUser.Email,
                                Token = accessToken,
                                RefreshToken = refreshToken
                            }
                        );
                    }
                    else
                    {
                        _logger.LogError("Failed to add user {Username} to ChatUser role. Errors: {Errors}", appUser.UserName, roleResult.Errors);
                        return StatusCode(500, roleResult.Errors);
                    }
                }
                else
                {
                    _logger.LogError("Failed to create user {Email}. Errors: {Errors}", appUser.Email, createdUserResult.Errors);
                    return StatusCode(500, createdUserResult.Errors);
                }
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

        
    }
}