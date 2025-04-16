using System.ComponentModel.DataAnnotations;

namespace SchoolProject.Models
{
    public class Module
    {
        [Key]
        public int ModuleID { get; set; }

        [Required]
        [StringLength(100)]
        public string ModuleName { get; set; }

        [Required]
        [Range(1, 52, ErrorMessage = "Duration must be between 1 and 52 weeks.")]
        public int Duration { get; set; }

        [Required]
        public ModuleType ModuleType { get; set; }

        [Required]
        public ModuleStatus ModuleStatus { get; set; }

        public ICollection<LecturerModule>? LecturerModules { get; set; }
        public ICollection<StudentModule>? StudentModules { get; set; }
    }

    public enum ModuleType
    {
        Core,
        Elective
    }

    public enum ModuleStatus
    {
        Active,
        Inactive
    }
}
