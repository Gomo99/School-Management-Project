using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class Assessment
    {
        [Key]
        public int AssessmentID { get; set; }

        [Required]
        public int StudentModuleID { get; set; }

        [Required]
        public int AssessmentTypeID { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Required]
        public AssessmentStatus AssessmentStatus { get; set; }

        // Navigation properties
        [ForeignKey("StudentModuleID")]
        public virtual StudentModule StudentModule { get; set; }

        [ForeignKey("AssessmentTypeID")]
        public virtual AssessmentType AssessmentType { get; set; }
    }



    public enum AssessmentStatus
    {
        Active,
        Inactive
    }
}
