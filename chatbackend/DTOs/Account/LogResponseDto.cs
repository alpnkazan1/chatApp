using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Account
{
    public class LogResponseDto
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
    }
}