using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace chatbackend.DTOs.Messages
{
    public class MessageReqDto
    {
        public Guid MessageId { get; set; }
        public Guid ChatId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string? MessageText { get; set; }
        public uint FileFlag { get; set; }
        public Guid? FileId { get; set; }
        public string? FileExtension { get; set; }
        public DateTime Timestamp { get; set; }
        public string? FileUrl { get; set; } // Add this property for the secured URL
    }
}