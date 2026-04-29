using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

        [Required]
        public int UserID { get; set; }           // Recipient

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public NotificationType Type { get; set; } = NotificationType.General;

        public int? ReferenceID { get; set; }      // e.g., AssessmentID for deep‑linking

        [ForeignKey("UserID")]
        public Account User { get; set; }
    }

    public enum NotificationType
    {
        General,
        AssessmentCreated,
        DeadlineReminder,
        AssessmentMissed,
        AssessmentCompleted,
        AssessmentRescheduled
    }
}