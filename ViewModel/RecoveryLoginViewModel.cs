using System.ComponentModel.DataAnnotations;

namespace SchoolProject.ViewModel
{
    public class RecoveryLoginViewModel
    {
        [Required]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; }
    }
}
