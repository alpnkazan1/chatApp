using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Chats
{
    public class CreateChatRequestDto
    {
        public string User1Id { get; set; }
        public string User2Id { get; set; }
        public string UserName1 { get; set; }
        public string UserName2 { get; set; }
    }
}