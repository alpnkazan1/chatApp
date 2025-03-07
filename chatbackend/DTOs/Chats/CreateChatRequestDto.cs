using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Chats
{
    public class CreateChatRequestDto
    {
        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }
        public string UserName1 { get; set; }
        public string UserName2 { get; set; }
    }
}