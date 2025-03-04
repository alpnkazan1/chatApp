using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chatbackend.Models
{
    [Table("Messages")]
    public class Message
    {
        public Guid MessageId { get; set; }
        public Chat Chat { get; set; }
        public Guid ChatId { get; set; }
        
        public Guid SenderId { get; set; }

        [ForeignKey("SenderId")]
        public User Sender { get; set; }

        public Guid ReceiverId { get; set; }

        [ForeignKey("ReceiverId")]
        public User Receiver { get; set; }
        [MaxLength(1000)]
        public string? MessageText { get; set; }

        /*
            0 -> No files attached
            1 -> Sound file
            2 -> Image file
            3 -> Rest
        */
        public uint FileFlag { get; set; } = 0; 
        public Guid? FileId { get; set; }
        public string? FileExtension { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}