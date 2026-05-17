using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Logging;
using Mosaik.Core.Notification;

namespace Mosaik.Modules.Circular.Services
{
    // C-03 (Plan 33 Faz 2) — Günlük tamim hatırlatma cron (09:00).
    // Dün UTC'de yayınlanan tamimler için henüz okumamış aktif kullanıcılara
    // in-app bildirim gönderir.
    // Okuma takibi: AuditLog.EventType='circular_read', TargetType='circular', TargetKey=CircularId.
    public class TamimReminderJob
    {
        private readonly DbContext _db;
        private readonly INotificationService _notifications;
        private readonly IAuditLog _audit;
        private readonly ILogger<TamimReminderJob> _logger;

        public TamimReminderJob(
            DbContext db,
            INotificationService notifications,
            IAuditLog audit,
            ILogger<TamimReminderJob> logger)
        {
            _db = db;
            _notifications = notifications;
            _audit = audit;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            // CompileCircularJob 17:00 Turkey = 14:00 UTC çalışır — "dün UTC" penceresi tüm olası
            // yayın saatlerini kapsar.
            var yesterdayStart = DateTime.UtcNow.Date.AddDays(-1);
            var yesterdayEnd   = DateTime.UtcNow.Date;

            var circulars = await _db.Set<Models.Circular>()
                .AsNoTracking()
                .Where(c => c.PublishedAt >= yesterdayStart && c.PublishedAt < yesterdayEnd)
                .ToListAsync(ct);

            if (circulars.Count == 0)
            {
                _logger.LogInformation("TamimReminderJob: Dün yayınlanan tamim yok, atlıyorum.");
                return;
            }

            int totalReminders = 0;

            foreach (var circular in circulars)
            {
                // Henüz okumamış aktif kullanıcılar: AuditLog'da circular_read kaydı olmayanlar.
                // EF Core SqlQuery FormattableString → otomatik parameterize (güvenli).
                var idStr = circular.Id.ToString();
                var unreadUsers = await _db.Database
                    .SqlQuery<TamimUnreadUserRow>($@"
                        SELECT u.UserId, u.Username, u.FullName, u.Email
                        FROM Users u
                        WHERE u.IsActive = 1
                          AND NOT EXISTS (
                              SELECT 1 FROM AuditLog a
                              WHERE a.Username   = u.Username
                                AND a.EventType  = 'circular_read'
                                AND a.TargetType = 'circular'
                                AND a.TargetKey  = {idStr}
                          )")
                    .ToListAsync(ct);

                if (unreadUsers.Count == 0)
                {
                    _logger.LogInformation(
                        "TamimReminderJob: {Number} — tüm kullanıcılar okumuş, atlıyorum.",
                        circular.CircularNumber);
                    continue;
                }

                var userIds   = unreadUsers.Select(u => u.UserId).ToList();
                var dateLabel = circular.CircularDate.ToString("dd MMMM yyyy");

                var sent = await _notifications.CreateBulkAsync(
                    userIds,
                    entityType:       "Circular",
                    entityId:         circular.Id,
                    title:            $"Hatırlatma: {circular.CircularNumber} henüz okunmadı",
                    message:          $"{dateLabel} tarihli tamimi henüz okumadınız.",
                    targetUrl:        $"/Circular/Circular/Details/{circular.Id}",
                    notificationType: "CircularReminder",
                    createdBy:        "system");

                totalReminders += sent;
                _logger.LogInformation(
                    "TamimReminderJob: {Number} — {Sent}/{Total} kullanıcıya hatırlatma gönderildi.",
                    circular.CircularNumber, sent, unreadUsers.Count);
            }

            await _audit.LogAsync(
                eventType:   "tamim_reminder_sent",
                targetType:  "system",
                targetKey:   yesterdayStart.ToString("yyyyMMdd"),
                description: $"{circulars.Count} tamim için {totalReminders} hatırlatma gönderildi.");

            _logger.LogInformation(
                "TamimReminderJob tamamlandı. Tamim: {Circular}, Bildirim: {Total}",
                circulars.Count, totalReminders);
        }
    }

    // SqlQueryRaw POCO — sadece bu job için, model'e kayıt gerekmez (EF Core 8+ destekler).
    internal class TamimUnreadUserRow
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
