using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using chatbackend.Models;

namespace chatbackend.Interfaces
{
    public interface ITokenService
    {
        string CreateToken(User user);
    }
}