using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Email;
using Mosaik.Core.Logging;
using Mosaik.Core.Notification;
using Mosaik.Core.Users;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34 Faz E S-21 + S-22 — Günlük SOP okuma hatırlatması (09:00 Europe/Istanbul).
    // Atanmış ama henüz onaylanmamış (ConfirmedAt NULL) read receipt'lar için:
    //   - daysRemaining ≤ 7 && ReminderSentCount = 0 → in-app push, count=1
    //   - daysRemaining ≤ 1 && ReminderSentCount = 1 → son uyarı in-app + email, count=2
    // Overdue (daysRemaining < 0) için ekstra push yok — admin dashboard'da takip.
    // Email IEmailService.IsEnabled=false ise sessiz skip (Plan 31/32).
    public class SopReadReminderJob
    {
        private readonly DbContext _db;
        private readonly INotificationService _notifications;
        private readonly IEmailService _email;
        private readonly IActiveUserDirectory _userDirectory;
        private readonly IAuditLog _audit;
        private readonly ILogger<SopReadReminderJob> _logger;

        public SopReadReminderJob(
            DbContext db,
            INotificationService notifications,
            IEmailService email,
            IActiveUserDirectory userDirectory,
            IAuditLog audit,
            ILogger<SopReadReminderJob> logger)
        {
            _db = db;
            _notifications = notifications;
            _email = email;
            _userDirectory = userDirectory;
            _audit = audit;
            _logger = logger;
        }

        public async Task<SopReminderResult> ExecuteAsync(CancellationToken ct = default)
        {
            var now = DateTime.UtcNow;

            // Bekleyen tüm receipt'lar — daysRemaining in-memory hesaplanır.
            var pending = await _db.Set<SopReadReceipt>()
                .Include(r => r.SopVersion)
                    .ThenInclude(v => v!.SopDocument)
                .Where(r => r.ConfirmedAt == null
                         && r.ReminderSentCount < 2
                         && r.SopVersion != null
                         && r.SopVersion.SopDocument != null)
                .ToListAsync(ct);

            if (pending.Count == 0)
            {
                _logger.LogInformation("SopReadReminderJob: bekleyen okuma yok.");
                return new SopReminderResult(0, 0, 0, 0);
            }

            // Receipt'ları version bazında grupla — aynı SOP için tek bulk notification.
            var sevenDayByVersion = new Dictionary<int, List<SopReadReceipt>>();
            var oneDayByVersion = new Dictionary<int, List<SopReadReceipt>>();

            foreach (var r in pending)
            {
                var deadline = r.AssignedAt.AddDays(r.SopVersion!.SopDocument!.ReadDeadlineDays);
                var daysRemaining = (deadline - now).TotalDays;

                if (daysRemaining <= 1 && r.ReminderSentCount < 2)
                {
                    if (!oneDayByVersion.TryGetValue(r.SopVersionId, out var list))
                        oneDayByVersion[r.SopVersionId] = list = new();
                    list.Add(r);
                }
                else if (daysRemaining <= 7 && r.ReminderSentCount == 0)
                {
                    if (!sevenDayByVersion.TryGetValue(r.SopVersionId, out var list))
                        sevenDayByVersion[r.SopVersionId] = list = new();
                    list.Add(r);
                }
            }

            var (sevenSent, _) = await DispatchAsync(sevenDayByVersion, isFinal: false, ct);
            var (oneSent, emailsSent) = await DispatchAsync(oneDayByVersion, isFinal: true, ct);

            if (sevenSent + oneSent > 0)
            {
                await _audit.LogAsync(
                    eventType: "sop_reminder_sent",
                    targetType: "system",
                    targetKey: now.ToString("yyyyMMdd"),
                    description: $"SOP hatırlatma: {sevenSent} (7-gün), {oneSent} (1-gün) bildirim, {emailsSent} email.");
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "SopReadReminderJob tamamlandı. 7-gün: {Seven}, 1-gün: {One}, email: {Email}, toplam version: {Versions}",
                sevenSent, oneSent, emailsSent, sevenDayByVersion.Count + oneDayByVersion.Count);

            return new SopReminderResult(
                Pending: pending.Count,
                SevenDayReminders: sevenSent,
                OneDayReminders: oneSent,
                EmailsSent: emailsSent);
        }

        // isFinal=true ise 1-gün son uyarı: in-app push + email (IsEnabled=true ise).
        // Email başarısızlığı reminder push'u bozmaz (best-effort, log warning).
        private async Task<(int notified, int emailsSent)> DispatchAsync(
            Dictionary<int, List<SopReadReceipt>> byVersion,
            bool isFinal,
            CancellationToken ct)
        {
            if (byVersion.Count == 0) return (0, 0);

            int notified = 0;
            int emailsSent = 0;

            foreach (var (versionId, receipts) in byVersion)
            {
                var first = receipts[0].SopVersion!;
                var doc = first.SopDocument!;
                var userIds = receipts.Select(r => r.UserId).Distinct().ToList();
                if (userIds.Count == 0) continue;

                var title = isFinal
                    ? $"Son uyarı: {doc.Title} v{first.VersionNumber}"
                    : $"Hatırlatma: {doc.Title} v{first.VersionNumber}";
                var message = isFinal
                    ? "SOP okuma süresi 1 günden az. Lütfen okuyup onaylayın."
                    : $"SOP okuma süresi {doc.ReadDeadlineDays} günle sınırlı, 1 hafta içinde dolacak.";

                var sent = await _notifications.CreateBulkAsync(
                    userIds: userIds,
                    entityType: "sop_version",
                    entityId: versionId,
                    title: title,
                    message: message,
                    targetUrl: $"/SOP/My/Read/{versionId}",
                    notificationType: isFinal ? "SopReminderFinal" : "SopReminder",
                    createdBy: "system");

                foreach (var r in receipts)
                {
                    r.ReminderSentCount = isFinal ? 2 : 1;
                }

                notified += sent;

                if (isFinal && _email.IsEnabled)
                {
                    emailsSent += await SendFinalEmailsAsync(userIds, doc, first, ct);
                }

                if (ct.IsCancellationRequested) break;
            }

            return (notified, emailsSent);
        }

        private async Task<int> SendFinalEmailsAsync(
            List<int> userIds,
            SopDocument doc,
            SopVersion version,
            CancellationToken ct)
        {
            Dictionary<int, string> contacts;
            try
            {
                contacts = await _userDirectory.GetUserEmailsAsync(userIds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SopReadReminderJob: email lookup hata VersionId={VersionId}", version.Id);
                return 0;
            }

            if (contacts.Count == 0) return 0;

            var subject = $"[Mosaik] Son uyarı — {doc.Title} v{version.VersionNumber} okuma süresi doluyor";
            var body = BuildFinalEmailHtml(doc, version);

            int sent = 0;
            foreach (var (userId, email) in contacts)
            {
                if (ct.IsCancellationRequested) break;
                try
                {
                    var result = await _email.SendAsync(email, subject, body, ct);
                    if (result.IsSuccess)
                    {
                        sent++;
                    }
                    else if (!result.WasSkipped)
                    {
                        _logger.LogWarning(
                            "SopReadReminderJob: email gönderilemedi UserId={UserId} VersionId={VersionId} Reason={Reason}",
                            userId, version.Id, result.ErrorDetail);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "SopReadReminderJob: email exception UserId={UserId} VersionId={VersionId}",
                        userId, version.Id);
                }
            }
            return sent;
        }

        private static string BuildFinalEmailHtml(SopDocument doc, SopVersion version)
        {
            var title = WebUtility.HtmlEncode(doc.Title);
            return $$"""
                <!DOCTYPE html>
                <html lang="tr">
                <head><meta charset="UTF-8"></head>
                <body style="font-family:-apple-system,Segoe UI,Arial,sans-serif;background:#f3f4f6;margin:0;padding:0;">
                  <div style="max-width:600px;margin:32px auto;background:#fff;border-radius:10px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,.08);">
                    <div style="background:#111827;padding:24px 32px;">
                      <h1 style="color:#dc2626;margin:0;font-size:22px;">Mosaik — Prosedür Hatırlatması</h1>
                    </div>
                    <div style="padding:28px 32px;color:#1f2937;font-size:15px;line-height:1.6;">
                      <p><strong>Son uyarı:</strong> <span style="background:#fee2e2;color:#b91c1c;padding:2px 8px;border-radius:4px;font-size:12px;font-weight:600;">1 günden az</span></p>
                      <p>{{title}} (v{{version.VersionNumber}}) prosedürünü okuma süreniz 1 günden az kaldı. Lütfen Mosaik'te okuyup onaylayın.</p>
                      <p>Bu e-posta otomatik gönderilmiştir, yanıtlamayın.</p>
                    </div>
                  </div>
                </body>
                </html>
                """;
        }
    }

    public record SopReminderResult(
        int Pending,
        int SevenDayReminders,
        int OneDayReminders,
        int EmailsSent);
}
