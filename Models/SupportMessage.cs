using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SchoolProject.Models
{
    public class SupportMessage
    {
        public int SupportMessageID { get; set; }

        [Required]
        public int SenderID { get; set; }

        [ForeignKey("SenderID")]
        public Account Sender { get; set; }

        [Required]
        public int ReceiverID { get; set; }

        [ForeignKey("ReceiverID")]
        public Account Receiver { get; set; }

        [Required]
        public string MessageText { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;

        public bool IsRead { get; set; } = false;
    }


}
