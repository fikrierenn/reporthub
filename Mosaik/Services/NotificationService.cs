using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Notification;
using Mosaik.Models;

namespace Mosaik.Services
{
    // Plan 17 Faz H — cross-modül bildirim servis impl.
    public class NotificationService : INotificationService
    {
        private readonly MosaikContext _context;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(MosaikContext context, ILogger<NotificationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Notification> CreateAsync(int userId, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
        {
            var n = new Notification
            {
                UserId = userId,
                EntityType = entityType,
                EntityId = entityId,
                Title = title,
                Message = message,
                TargetUrl = targetUrl,
                NotificationType = notificationType,
                CreatedBy = createdBy
            };
            _context.Notifications.Add(n);
            await _context.SaveChangesAsync();
            return n;
        }

        public async Task<int> CreateBulkAsync(IEnumerable<int> userIds, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
        {
            var now = DateTime.UtcNow;
            var batch = userIds.Distinct().Select(uid => new Notification
            {
                UserId = uid,
                EntityType = entityType,
                EntityId = entityId,
                Title = title,
                Message = message,
                TargetUrl = targetUrl,
                NotificationType = notificationType,
                CreatedAt = now,
                CreatedBy = createdBy
            }).ToList();

            if (batch.Count == 0) return 0;

            _context.Notifications.AddRange(batch);
            await _context.SaveChangesAsync();
            _logger.LogInformation("NotificationService bulk insert {Count} entityType={EntityType}", batch.Count, entityType);
            return batch.Count;
        }

        public async Task<int> NotifyAllActiveUsersAsync(string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
        {
            var userIds = await _context.Users.AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => u.UserId)
                .ToListAsync();
            return await CreateBulkAsync(userIds, entityType, entityId, title, message, targetUrl, notificationType, createdBy);
        }

        public Task<int> GetUnreadCountAsync(int userId) =>
            _context.Notifications.AsNoTracking()
                .Where(n => n.UserId == userId && !n.IsRead)
                .CountAsync();

        public Task<List<Notification>> GetRecentAsync(int userId, int take = 30, bool unreadOnly = false)
        {
            var q = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly) q = q.Where(n => !n.IsRead);
            return q.OrderByDescending(n => n.CreatedAt).Take(take).ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var n = await _context.Notifications.FirstOrDefaultAsync(x => x.Id == notificationId && x.UserId == userId);
            if (n == null) return false;
            if (n.IsRead) return true;
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> MarkAllAsReadAsync(int userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();
            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }
            if (unread.Count > 0) await _context.SaveChangesAsync();
            return unread.Count;
        }
    }
}
