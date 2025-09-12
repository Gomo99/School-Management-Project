using SchoolProject.ViewModel;

namespace SchoolProject.Service
{
    public interface IMessageService
    {
        Task<bool> SendMessageAsync(int senderId, int receiverId, string content);
        Task<List<MessageViewModel>> GetInboxAsync(int userId);
        Task<List<MessageViewModel>> GetSentMessagesAsync(int userId);
        Task<MessageViewModel> GetMessageAsync(int messageId, int userId);
        Task<bool> MarkAsReadAsync(int messageId, int userId);
        Task<bool> DeleteMessageAsync(int messageId, int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
