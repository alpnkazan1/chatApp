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

        public string SenderId { get; set; }
        public User Sender { get; set; }

        public string? MessageText { get; set; }

        public Guid? PhotoId { get; set; }

        public Guid? SoundId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}