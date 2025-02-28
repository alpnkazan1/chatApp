using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Messages
{
    public class MessageSendDto
    {
        public Guid ChatId { get; set; }
        public Guid ReceiverId { get; set; }
        public string? MessageText { get; set; }
        public uint FileFlag { get; set; } = 0;  // Default to 0 (No file)
        public Guid? FileId { get; set; }
        public string? FileExtension { get; set; }
    }

}