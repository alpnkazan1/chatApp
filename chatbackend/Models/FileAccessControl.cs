using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace chatbackend.Models
{
    [Table("ACL")]
    public class FileAccessControl
    {
        [Key]
        public int Id { get; set; } // Auto-generated primary key

        [ForeignKey("UserId")]
        public User User { get; set; }
        public Guid UserId { get; set; } // Foreign key to User

        [ForeignKey("MessageId")]
        public Message Message { get; set; }
        public Guid MessageId { get; set; } // Foreign key to Message

        public Guid FileId { get; set; } // Foreign key to Message (using FileId)
        public string FolderName { get; set; } // The folder name where the file is stored
        public AccessType AccessType { get; set; } // The type of access (e.g., "Read", "Write")
    }

    // Define an enum for the different access types
    public enum AccessType
    {
        Read,
        Write,
        Full
    }
}