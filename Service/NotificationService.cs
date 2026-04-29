using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;

namespace SchoolProject.Service
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRealTimeMessageService _realTime;

        public NotificationService(ApplicationDbContext context, IRealTimeMessageService realTime)
        {
            _context = context;
            _realTime = realTime;
        }

        // Create and push a notification
        public async Task CreateAsync(int userId, string message, string? type = null)
        {
            var notification = new Notification
            {
                UserID = userId,
                Message = message,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Real-time push
            if (type == "deadline_reminder")
            {
                await _realTime.NotifyDeadlineReminder(userId, message);
            }
            else
            {
                await _realTime.NotifyAssessmentCreated(userId, message);
            }
        }

        // Get unread count for a user
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .CountAsync();
        }

        // Get recent notifications for dropdown
        public async Task<List<Notification>> GetRecentAsync(int userId, int take = 10)
        {
            return await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        // Mark a notification as read
        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notif = await _context.Notifications.FindAsync(notificationId);
            if (notif != null && notif.UserID == userId)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        // Mark all as read
        public async Task MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
        }
    }
}