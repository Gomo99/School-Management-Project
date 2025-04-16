using System.ComponentModel.DataAnnotations;

namespace SchoolProject.Models
{
    public class AssessmentType
    {
        [Key]
        public int AssessmentTypeID { get; set; }

        [Required]
        [StringLength(100)]  // Adjust max length as per your requirement
        public string AssessmentTypeDescription { get; set; }

        [Required]
        public AssessmentTypeStatus AssessmentTypeStatus { get; set; }
    }



    public enum AssessmentTypeStatus
    {
        Active,
        Inactive
    }
}
