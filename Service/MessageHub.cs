using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace SchoolProject.Service
{
    public class MessageHub : Hub
    {
        private static readonly ConcurrentDictionary<int, string> _userConnections = new();

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserIdFromContext();
            if (userId > 0)
            {
                _userConnections[userId] = Context.ConnectionId;
                await Clients.All.SendAsync("UserOnlineStatusChanged", userId, true);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserIdFromContext();
            if (userId > 0 && _userConnections.TryRemove(userId, out _))
            {
                await Clients.All.SendAsync("UserOnlineStatusChanged", userId, false);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinUserGroup(int userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }

        public async Task StartTyping(int conversationId, int userId)
        {
            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("UserTyping", userId, true);
        }

        public async Task StopTyping(int conversationId, int userId)
        {
            await Clients.GroupExcept($"conversation_{conversationId}", Context.ConnectionId)
                .SendAsync("UserTyping", userId, false);
        }

        private int GetUserIdFromContext()
        {
            var httpContext = Context.GetHttpContext();
            var userIdClaim = httpContext?.User.FindFirst("UserID");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        public static string? GetConnectionId(int userId)
        {
            _userConnections.TryGetValue(userId, out var connectionId);
            return connectionId;
        }
    }
}
