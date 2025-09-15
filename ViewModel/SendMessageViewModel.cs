using System.ComponentModel.DataAnnotations;

namespace SchoolProject.ViewModel
{
    public class SendMessageViewModel
    {
        [Required]
        [Display(Name = "Recipient")]
        public int ReceiverId { get; set; }

        [Required]
        [MaxLength(500)]
        [Display(Name = "Message")]
        public string Content { get; set; }

    }
}
