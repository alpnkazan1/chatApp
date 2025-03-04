using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace chatbackend.Models
{
    public class User : IdentityUser<Guid>
    {
        [Key]
        public override Guid Id { get; set; } = Guid.NewGuid();
        public Guid? AvatarId { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; } 
        public string? LastSeen { get; set; }
    }
}