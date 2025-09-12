using Microsoft.EntityFrameworkCore;
using SchoolProject.Data;
using SchoolProject.Models;
using SchoolProject.ViewModel;

namespace SchoolProject.Service
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> SendMessageAsync(int senderId, int receiverId, string content)
        {
            if (senderId == receiverId)
                return false;

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
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
                IsRead = message.ReadAt.HasValue
            };
        }

        public async Task<bool> MarkAsReadAsync(int messageId, int userId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ReceiverId == userId);

            if (message == null || message.ReadAt.HasValue)
                return false;

            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
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

            // If both parties deleted, remove from database
            if (message.IsDeletedBySender && message.IsDeletedByReceiver)
                _context.Messages.Remove(message);

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
