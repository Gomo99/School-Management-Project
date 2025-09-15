namespace SchoolProject.ViewModel
{
    public class MessageViewModel
    {
        public int MessageId { get; set; }
        public string SenderName { get; set; }
        public string ReceiverName { get; set; }
        public string Content { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsRead { get; set; }
     
        public int ReceiverId { get; set; }
        public int SenderId { get; set; }


    }
}

