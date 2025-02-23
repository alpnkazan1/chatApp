using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.Models;

namespace chatbackend.Interfaces
{
    public interface ITokenService
    {
        Task<string> CreateToken(User user);
        Task<string?> RefreshTokenAsync(string refreshToken);
        Task<string> GenerateRefreshTokenAsync();
        Task RevokeTokenAsync(string token);
    }
}