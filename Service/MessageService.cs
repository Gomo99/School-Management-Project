using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.ViewModel;

namespace SchoolProject.Service
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRealTimeMessageService _realTimeService;

        public MessageService(ApplicationDbContext context, IRealTimeMessageService realTimeService
           )
        {
            _context = context;
            _realTimeService = realTimeService;
           
        }

        public async Task<bool> SendMessageAsync(int senderId, int receiverId, string content)
        {
            // 1) create message and save to get MessageId
            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(message);
            var saved = await _context.SaveChangesAsync() > 0;
            if (!saved)
                return false;


            // 3) Reload the message including attachments (and optionally sender/receiver) to build a clean object to broadcast
            var messageToBroadcast = await _context.Messages
                .Include(m => m.Sender)   // optional: include sender info if your real-time consumers need it
                .FirstOrDefaultAsync(m => m.MessageId == message.MessageId);

            // 4) Notify receiver in real-time (single notification)
            await _realTimeService.NotifyNewMessage(receiverId, messageToBroadcast);

            // 5) Update unread count (declare once)
            var unreadCount = await GetUnreadCountAsync(receiverId);
            await _realTimeService.UpdateUnreadCount(receiverId, unreadCount);

            return true;
        }












        public async Task<List<MessageViewModel>> GetInboxAsync(int userId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == userId && !m.IsDeletedByReceiver)
                .Include(m => m.Sender)
                .OrderByDescending(m => m.SentAt)
                .Select(m => new MessageViewModel
                {
                    MessageId = m.MessageId,
                    SenderName = $"{m.Sender.Name} {m.Sender.Surname}",
                    Content = m.Content,
                    SentAt = m.SentAt,
                    ReadAt = m.ReadAt,
                    IsRead = m.ReadAt.HasValue
                })
                .ToListAsync();
        }

        public async Task<List<MessageViewModel>> GetSentMessagesAsync(int userId)
        {
            return await _context.Messages
                .Where(m => m.SenderId == userId && !m.IsDeletedBySender)
                .Include(m => m.Receiver)
                .OrderByDescending(m => m.SentAt)
                .Select(m => new MessageViewModel
                {
                    MessageId = m.MessageId,
                    ReceiverName = $"{m.Receiver.Name} {m.Receiver.Surname}",
                    Content = m.Content,
                    SentAt = m.SentAt,
                    ReadAt = m.ReadAt,
                    IsRead = m.ReadAt.HasValue
                })
                .ToListAsync();
        }

        public async Task<MessageViewModel> GetMessageAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.MessageId == messageId &&
                    (m.SenderId == userId || m.ReceiverId == userId));

            if (message == null) return null;

            // Mark as read if receiver is viewing
            if (message.ReceiverId == userId && !message.ReadAt.HasValue)
            {
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return new MessageViewModel
            {
                MessageId = message.MessageId,
                SenderName = $"{message.Sender.Name} {message.Sender.Surname}",
                ReceiverName = $"{message.Receiver.Name} {message.Receiver.Surname}",
                Content = message.Content,
                SentAt = message.SentAt,
                ReadAt = message.ReadAt,
                IsRead = message.ReadAt.HasValue,
               
            };

        }


        public async Task<bool> MarkAsReadAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ReceiverId == userId);

            if (message != null && !message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Notify in real-time
                await _realTimeService.NotifyMessageRead(messageId, userId);

                // Update unread count
                var unreadCount = await GetUnreadCountAsync(userId);
                await _realTimeService.UpdateUnreadCount(userId, unreadCount);

                return true; // ✅ message was successfully marked as read
            }

            return false; // ❌ nothing changed
        }





        public async Task<bool> DeleteMessageAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId &&
                    (m.SenderId == userId || m.ReceiverId == userId));

            if (message == null) return false;

            if (message.SenderId == userId)
                message.IsDeletedBySender = true;
            else
                message.IsDeletedByReceiver = true;

           


            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Messages
                .CountAsync(m => m.ReceiverId == userId &&
                    !m.ReadAt.HasValue &&
                    !m.IsDeletedByReceiver);
        }
    }
}
