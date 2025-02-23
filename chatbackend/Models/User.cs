using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace chatbackend.Models
{
    public class User : IdentityUser
    {
        public Guid AvatarId { get; set; }
    }
}