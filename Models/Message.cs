// Models/Message.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class Message
    {
        [Key]
        public int MessageId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Content { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReadAt { get; set; }

        [Required]
        public bool IsDeletedBySender { get; set; } = false;

        [Required]
        public bool IsDeletedByReceiver { get; set; } = false;

        // Navigation properties
        [ForeignKey("SenderId")]
        public virtual Account Sender { get; set; }

        [ForeignKey("ReceiverId")]
        public virtual Account Receiver { get; set; }
    }

   

  
}