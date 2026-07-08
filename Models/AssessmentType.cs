using System.ComponentModel.DataAnnotations;

namespace SchoolProject.Models
{
    public class AssessmentType
    {
        [Key]
        public int AssessmentTypeID { get; set; }

        [Required]
        [StringLength(100)]
        public string AssessmentTypeDescription { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}