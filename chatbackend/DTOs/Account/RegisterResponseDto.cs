using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Account
{
    public class RegisterResponseDto
    {
        public string? Email {get; set;}
        public string? UserName { get; set;}
    }
}