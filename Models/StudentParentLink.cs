using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolProject.Models
{
    public class StudentParentLink
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ParentUserId { get; set; }

        [Required]
        public int StudentUserId { get; set; }

        [ForeignKey("ParentUserId")]
        public virtual Account Parent { get; set; }

        [ForeignKey("StudentUserId")]
        public virtual Account Student { get; set; }
    }
}