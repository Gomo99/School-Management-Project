using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            return int.Parse(User.FindFirstValue("UserID"));
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            int userId = GetCurrentUserId();
            var notifs = await _context.Notifications
                .Where(n => n.UserID == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new {
                    n.NotificationID,
                    n.Message,
                    n.CreatedAt,
                    n.IsRead
                })
                .ToListAsync();
            return Json(notifs);
        }

        [HttpPost]
        public async Task<IActionResult> MarkNotificationRead(int id)
        {
            int userId = GetCurrentUserId();
            var notif = await _context.Notifications.FindAsync(id);
            if (notif != null && notif.UserID == userId)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllNotificationsRead()
        {
            int userId = GetCurrentUserId();
            var unread = await _context.Notifications
                .Where(n => n.UserID == userId && !n.IsRead)
                .ToListAsync();
            unread.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}