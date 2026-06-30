using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mosaik.Core.Email;
using Mosaik.Core.Users;
using Mosaik.Models;
using Mosaik.Services.Email;

namespace Mosaik.Services
{
    // Plan 54 M5 (M1'den devralındı) — günlük okunmamış bildirim digest'i.
    // Her kullanıcının okunmamış bildirimlerini tek e-posta özetine toplar (volume azaltma).
    // SMTP kapalıysa sessizce atlar (IEmailService.IsEnabled). Günlük cron — dedup gerekmez.
    public class NotificationDigestJob
    {
        private const int RecentTitleCount = 5;

        private readonly MosaikContext _db;
        private readonly IEmailService _email;
        private readonly IActiveUserDirectory _userDirectory;
        private readonly SmtpSettings _smtp;
        private readonly ILogger<NotificationDigestJob> _logger;

        public NotificationDigestJob(
            MosaikContext db,
            IEmailService email,
            IActiveUserDirectory userDirectory,
            IOptions<SmtpSettings> smtpOptions,
            ILogger<NotificationDigestJob> logger)
        {
            _db = db;
            _email = email;
            _userDirectory = userDirectory;
            _smtp = smtpOptions.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            if (!_email.IsEnabled)
            {
                _logger.LogInformation("NotificationDigestJob: SMTP kapalı, atlanıyor.");
                return;
            }

            // Okunmamış bildirimleri çek (kullanıcı bazlı gruplama in-memory).
            var unread = await _db.Notifications.AsNoTracking()
                .Where(n => !n.IsRead)
                .Select(n => new { n.UserId, n.Title, n.CreatedAt })
                .ToListAsync(ct);

            if (unread.Count == 0)
            {
                _logger.LogInformation("NotificationDigestJob: okunmamış bildirim yok.");
                return;
            }

            var groups = unread
                .GroupBy(n => n.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count(),
                    RecentTitles = g.OrderByDescending(x => x.CreatedAt)
                        .Take(RecentTitleCount)
                        .Select(x => x.Title)
                        .ToList()
                })
                .ToList();

            var emails = await _userDirectory.GetUserEmailsAsync(groups.Select(g => g.UserId));

            int sent = 0, skipped = 0, failed = 0;
            foreach (var g in groups)
            {
                if (!emails.TryGetValue(g.UserId, out var to) || string.IsNullOrWhiteSpace(to))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    var html = EmailTemplates.NotificationDigest(g.Count, g.RecentTitles, _smtp.AppUrl);
                    var result = await _email.SendAsync(to, $"Mosaik — {g.Count} okunmamış bildirim", html, ct);
                    if (result.IsSuccess)
                    {
                        sent++;
                    }
                    else
                    {
                        failed++;
                        _logger.LogWarning("NotificationDigestJob: UserId={Id} digest reddedildi. Kod={Code}",
                            g.UserId, result.ErrorCode);
                    }
                }
                catch (Exception ex)
                {
                    failed++;
                    _logger.LogError(ex, "NotificationDigestJob: UserId={Id} digest gönderilemedi.", g.UserId);
                }
            }

            _logger.LogInformation(
                "NotificationDigestJob tamamlandı. Kullanıcı: {Total}, Gönderilen: {Sent}, Atlanan: {Skipped}, Hata: {Failed}",
                groups.Count, sent, skipped, failed);
        }
    }
}
