using SchoolProject.Models;

// Services/IRealTimeMessageService.cs
namespace SchoolProject.Service
{
    public interface IRealTimeMessageService
    {
        Task NotifyNewMessage(int receiverId, Message message);
        Task NotifyMessageRead(int messageId, int readerId);
        Task UpdateUnreadCount(int userId, int count);
        Task NotifyUserOnlineStatus(int userId, bool isOnline);
        Task NotifyTyping(int conversationId, int userId, bool isTyping); // Add this method
    }
}
