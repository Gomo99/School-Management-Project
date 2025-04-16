using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class LecturerModule
    {
        [Key]
        public int LecturerModuleID { get; set; }

        [Required]
        public int ModuleID { get; set; }

        [ForeignKey("ModuleID")]
        public Module Module { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public Account Lecturer { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Required]
        public ModLecturerStatus ModLecturerStatus { get; set; }
    }

    public enum ModLecturerStatus
    {
        Active,
        Inactive
    }
}
