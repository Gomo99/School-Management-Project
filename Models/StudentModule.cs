using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class StudentModule
    {
        // Surrogate Key
        [Key]
        public int StudentModuleID { get; set; }

        // Foreign Key: Reference to LecturerModule
        [Required]
        public int LecturerModuleID { get; set; }

        // Navigation Property for LecturerModule
        [ForeignKey("LecturerModuleID")]
        public LecturerModule LecturerModule { get; set; }

        // Foreign Key: Reference to User (Student)
        [Required]
        public int UserID { get; set; }

        // Navigation Property for Account (Student)
        [ForeignKey("UserID")]
        public Account Student { get; set; }

        // Date of enrollment
        public DateTime Date { get; set; } = DateTime.Now;

        // Status of enrollment (e.g., Active, Completed, Inactive)
        [Required]
        public StudModStatus StudModStatus { get; set; }
    }

    // Enum for enrollment status
    public enum StudModStatus
    {
        Active,
        Inactive,
        Completed
    }
}
