using SchoolProject.Status;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class StudentModule
    {
        [Key]
        public int StudentModuleID { get; set; }

        [Required]
        public int LecturerModuleID { get; set; }

        [ForeignKey("LecturerModuleID")]
        public LecturerModule LecturerModule { get; set; }

        [Required]
        public int UserID { get; set; }

        [ForeignKey("UserID")]
        public Account Student { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public StudModStatus StudModStatus { get; set; }
    }
}