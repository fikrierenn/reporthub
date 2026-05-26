using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Email;
using Mosaik.Core.Messaging;
using Mosaik.Core.Notification;
using Mosaik.Models;

namespace Mosaik.Services.Messaging
{
    // IMessenger impl — in-app notification (her zaman) + HTML email (HtmlEmail != null && SMTP aktif).
    // Caller şablon seçimini yapar (EmailTemplates.XXX), bu servis kanalları birleştirir.
    public class MessengerService : IMessenger
    {
        private readonly INotificationService _notifications;
        private readonly IEmailService _email;
        private readonly MosaikContext _db;
        private readonly ILogger<MessengerService> _logger;

        public MessengerService(
            INotificationService notifications,
            IEmailService email,
            MosaikContext db,
            ILogger<MessengerService> logger)
        {
            _notifications = notifications;
            _email = email;
            _db = db;
            _logger = logger;
        }

        public Task SendAsync(int userId, MessengerPayload payload, CancellationToken ct = default)
            => SendBulkAsync([userId], payload, ct);

        public async Task SendBulkAsync(IReadOnlyList<int> userIds, MessengerPayload payload, CancellationToken ct = default)
        {
            if (userIds.Count == 0) return;

            var entityType = payload.EntityType ?? "system";

            if (payload.ExternalKey != null)
            {
                await _notifications.CreateBulkIfNotExistsAsync(
                    payload.ExternalKey,
                    userIds,
                    entityType,
                    payload.EntityId,
                    payload.Title,
                    payload.Body,
                    payload.TargetUrl,
                    payload.NotificationType,
                    createdBy: "system");
            }
            else
            {
                await _notifications.CreateBulkAsync(
                    userIds,
                    entityType,
                    payload.EntityId,
                    payload.Title,
                    payload.Body,
                    payload.TargetUrl,
                    payload.NotificationType,
                    createdBy: "system");
            }

            if (payload.HtmlEmail is null || !_email.IsEnabled) return;

            var emails = await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.UserId)
                         && u.IsActive
                         && u.Email != null
                         && u.Email != "")
                .Select(u => u.Email!)
                .ToListAsync(ct);

            if (emails.Count == 0) return;

            var result = await _email.SendBulkAsync(emails, $"[Mosaik] {payload.Title}", payload.HtmlEmail, ct);
            if (!result.AllSucceeded)
            {
                _logger.LogWarning(
                    "MessengerService email: {Failed} alıcıya gönderilemedi. Title={Title}",
                    result.Failures.Count, payload.Title);
            }
        }
    }
}
