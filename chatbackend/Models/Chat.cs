using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace chatbackend.Models
{
    [Table("Chats")]
    public class Chat
    {
        public Guid ChatId { get; set; }

        public Guid User1Id { get; set; }

        public Guid User2Id { get; set; }

        [Required]
        public required User User1 { get; set; }
        [Required]
        public required User User2 { get; set; }

        [Required]
        public required string UserName1 { get; set; }
        [Required]
        public required string UserName2 { get; set; }

        /*
            0 -> No blocking
            1 -> User1 blocked User2
            2 -> User2 blocked User1
            3 -> Both blocked each other
        */
        public uint BlockFlag { get; set; } = 0;

        public Guid LastMessage { get; set; }

        public DateTime? LastMessageTime { get; set; }
    }
}
