using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Chats
{
    public class ChatCreatedDto
    {
        public string ChatId { get; set; }
        public string User1Id { get; set; }
        public string User2Id { get; set; }
    }
}