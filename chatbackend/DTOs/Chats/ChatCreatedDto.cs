using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Chats
{
    public class ChatCreatedDto
    {
        public Guid ChatId { get; set; }
        public Guid User1Id { get; set; }
        public Guid User2Id { get; set; }
    }
}