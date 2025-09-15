// Services/RealTimeMessageService.cs
using Microsoft.AspNetCore.SignalR;
using SchoolProject.Models;

namespace SchoolProject.Service
{
    public class RealTimeMessageService : IRealTimeMessageService
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public RealTimeMessageService(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyNewMessage(int receiverId, Message message)
        {
            await _hubContext.Clients.Group($"user_{receiverId}")
                .SendAsync("ReceiveMessage", message);
        }

        public async Task NotifyMessageRead(int messageId, int readerId)
        {
            await _hubContext.Clients.All
                .SendAsync("MessageRead", messageId, readerId);
        }

        public async Task UpdateUnreadCount(int userId, int count)
        {
            await _hubContext.Clients.Group($"user_{userId}")
                .SendAsync("UpdateUnreadCount", count);
        }

        public async Task NotifyUserOnlineStatus(int userId, bool isOnline)
        {
            await _hubContext.Clients.All
                .SendAsync("UserOnlineStatusChanged", userId, isOnline);
        }

        public async Task NotifyTyping(int conversationId, int userId, bool isTyping)
        {
            await _hubContext.Clients.GroupExcept($"conversation_{conversationId}", MessageHub.GetConnectionId(userId))
                .SendAsync("UserTyping", userId, isTyping);
        }
    }
}