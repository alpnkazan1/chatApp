using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using chatbackend.Data;
using chatbackend.Interfaces;
using chatbackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace chatbackend.Service
{
    public class TokenService : ITokenService
    {
        private readonly ApplicationDBContext _context;
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;
        public TokenService(ApplicationDBContext context, IConfiguration config)
        {
            _config = config;
            _context = context;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SigningKey"]));
        }
        public string CreateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.GivenName, user.UserName)
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };

            //Creates security token using descriptor from above
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            //Return token as string
            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<string?> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return null;

            //Check for refresh token in database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            
            if (user == null || user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return null; // Token is invalid or expired

            // Generate a new access token
            var newToken = CreateToken(user);

            // Generate a new refresh token
            user.RefreshToken = GenerateRefreshToken();
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(1);

            // Save the new refresh token to the database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return newToken;
        }

        public async Task RevokeTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            // Find the user with the given refresh token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == token);

            if (user == null)
                return; // Token not found, nothing to revoke

            // Clear the refresh token and expire it immediately
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = DateTime.UtcNow;

            // Save changes to the database
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

    }
}
