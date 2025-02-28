using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using chatbackend.Models;

namespace chatbackend.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
        Task<string?> RefreshTokenAsync(string refreshToken);
        string GenerateRefreshToken();
        Task RevokeTokenAsync(string token);
        public ClaimsPrincipal ValidateToken(string token);
    }
}