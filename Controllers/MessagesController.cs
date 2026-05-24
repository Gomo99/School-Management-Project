using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.Service;
using SchoolProject.ViewModel;
using System.Security.Claims;

namespace SchoolProject.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMessageService _messageService;
        private readonly IRealTimeMessageService _realTimeService;

        public MessagesController(
            ApplicationDbContext context,
            IMessageService messageService,
            IRealTimeMessageService realTimeService)
        {
            _context = context;
            _messageService = messageService;
            _realTimeService = realTimeService;
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("UserID");
            return userIdClaim != null ? int.Parse(userIdClaim.Value) : 0;
        }

        // GET: Messages/Inbox
        [HttpGet]
        public async Task<IActionResult> Inbox()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var messages = await _messageService.GetInboxAsync(userId);
            ViewBag.UnreadCount = await _messageService.GetUnreadCountAsync(userId);
            ViewBag.CurrentUserId = userId;

            return View(messages);
        }

        // GET: Messages/Sent
        [HttpGet]
        public async Task<IActionResult> Sent()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var messages = await _messageService.GetSentMessagesAsync(userId);
            return View(messages);
        }

        // GET: Messages/ViewMessage/5
        [HttpGet]
        public async Task<IActionResult> ViewMessage(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var message = await _messageService.GetMessageAsync(id, userId);
            if (message == null)
            {
                TempData["ErrorMessage"] = "Message not found.";
                return RedirectToAction("Inbox");
            }

            ViewBag.CurrentUserId = userId;
            ViewBag.SenderId = message.SenderId;

            return View(message);
        }

        // GET: Messages/Compose
        [HttpGet]
        public IActionResult Compose(int? replyTo = null, int? forward = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var model = new SendMessageViewModel();

            if (replyTo.HasValue)
            {
                var originalMessage = _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefault(m => m.MessageId == replyTo.Value && m.ReceiverId == userId);

                if (originalMessage != null)
                {
                    model.ReceiverId = originalMessage.SenderId;

                    ViewBag.OriginalMessage = new
                    {
                        SenderName = $"{originalMessage.Sender.Name} {originalMessage.Sender.Surname}",
                        SentAt = originalMessage.SentAt,
                        Content = originalMessage.Content,
                        IsForward = false
                    };
                    ViewBag.ReplyToMessageId = originalMessage.MessageId;
                    ViewBag.IsReply = true;
                }
                else
                {
                    TempData["ErrorMessage"] = "Message not found or you don't have permission to reply to this message.";
                }
            }
            else if (forward.HasValue)
            {
                var originalMessage = _context.Messages
                    .Include(m => m.Sender)
                    .FirstOrDefault(m => m.MessageId == forward.Value && (m.SenderId == userId || m.ReceiverId == userId));

                if (originalMessage != null)
                {
                    ViewBag.OriginalMessage = new
                    {
                        SenderName = $"{originalMessage.Sender.Name} {originalMessage.Sender.Surname}",
                        SentAt = originalMessage.SentAt,
                        Content = originalMessage.Content,
                        IsForward = true
                    };
                    ViewBag.ForwardMessageId = originalMessage.MessageId;
                    ViewBag.IsForward = true;

                    model.Content = $"\n\n--- Forwarded Message ---\n" +
                                   $"From: {originalMessage.Sender.Name} {originalMessage.Sender.Surname}\n" +
                                   $"Date: {originalMessage.SentAt:MMM dd, yyyy at h:mm tt}\n\n" +
                                   $"{originalMessage.Content}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Message not found or you don't have permission to forward this message.";
                }
            }

            ViewBag.Users = _context.Accounts
                .Where(u => u.UserID != userId && u.UserStatus == Status.UserStatus.Active)
                .Select(u => new { u.UserID, FullName = $"{u.Name} {u.Surname} ({u.Email})" })
                .ToList();

            return View(model);
        }

        // POST: Messages/Compose
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Compose(SendMessageViewModel model, int? replyToMessageId = null, int? forwardMessageId = null)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Users = _context.Accounts
                    .Where(u => u.UserID != userId && u.UserStatus == Status.UserStatus.Active)
                    .Select(u => new { u.UserID, FullName = $"{u.Name} {u.Surname} ({u.Email})" })
                    .ToList();

                if (replyToMessageId.HasValue)
                {
                    ViewBag.IsReply = true;
                    var originalMessage = _context.Messages
                        .Include(m => m.Sender)
                        .FirstOrDefault(m => m.MessageId == replyToMessageId.Value);

                    if (originalMessage != null)
                    {
                        ViewBag.OriginalMessage = new
                        {
                            SenderName = $"{originalMessage.Sender.Name} {originalMessage.Sender.Surname}",
                            SentAt = originalMessage.SentAt,
                            Content = originalMessage.Content,
                            IsForward = false
                        };
                        ViewBag.ReplyToMessageId = replyToMessageId;
                    }
                }
                else if (forwardMessageId.HasValue)
                {
                    ViewBag.IsForward = true;
                    var originalMessage = _context.Messages
                        .Include(m => m.Sender)
                        .FirstOrDefault(m => m.MessageId == forwardMessageId.Value);

                    if (originalMessage != null)
                    {
                        ViewBag.OriginalMessage = new
                        {
                            SenderName = $"{originalMessage.Sender.Name} {originalMessage.Sender.Surname}",
                            SentAt = originalMessage.SentAt,
                            Content = originalMessage.Content,
                            IsForward = true
                        };
                        ViewBag.ForwardMessageId = forwardMessageId;
                    }
                }

                return View(model);
            }

            if (replyToMessageId.HasValue)
            {
                var originalMessage = await _context.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == replyToMessageId.Value && m.ReceiverId == userId);

                if (originalMessage == null)
                {
                    TempData["ErrorMessage"] = "Cannot reply to this message.";
                    return RedirectToAction("Inbox");
                }

                model.ReceiverId = originalMessage.SenderId;
            }

            var success = await _messageService.SendMessageAsync(userId, model.ReceiverId, model.Content);

            if (success)
            {
                string successMessage = replyToMessageId.HasValue ? "Reply sent successfully!" :
                                      forwardMessageId.HasValue ? "Message forwarded successfully!" :
                                      "Message sent successfully!";

                TempData["SuccessMessage"] = successMessage;
                return RedirectToAction("Sent");
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send message. Please try again.";
                return View(model);
            }
        }

        // POST: Messages/DeleteMessage/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            var success = await _messageService.DeleteMessageAsync(id, userId);

            if (success)
                TempData["SuccessMessage"] = "Message deleted successfully!";
            else
                TempData["ErrorMessage"] = "Failed to delete message.";

            return RedirectToAction("Inbox");
        }

        // POST: Messages/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Account");

            await _messageService.MarkAsReadAsync(id, userId);
            return Ok();
        }

        // GET: Messages/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(0);

            var count = await _messageService.GetUnreadCountAsync(userId);
            return Json(count);
        }

        // POST: Messages/StartTyping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StartTyping(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            await _realTimeService.NotifyTyping(conversationId, userId, true);
            return Ok();
        }

        // POST: Messages/StopTyping
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> StopTyping(int conversationId)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Unauthorized();

            await _realTimeService.NotifyTyping(conversationId, userId, false);
            return Ok();
        }
    }
}