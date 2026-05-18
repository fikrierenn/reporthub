using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mosaik.Core.Domain;
using Mosaik.Core.Email;
using Mosaik.Core.Notification;
using Mosaik.Models;
using Mosaik.Services.Email;

namespace Mosaik.Services
{
    // C-01 (Plan 33 Faz 2) — Günlük hatırlatma cron (09:00).
    // Kapsamı:
    //   1. Pending ContractObligations: DueDate - bugün <= ReminderDays → hatırlatma bildirimi
    //   2. Pending ContractObligations: DueDate < bugün → gecikme bildirimi
    //   3. Her ikisi için: in-app Notification + SMTP email (Enabled ise)
    //   4. ReminderSentAt = now() → aynı yükümlülük için tekrar gönderim olmaz
    // Calendar modülü (Plan 22) ve Tamim hatırlatması (C-03) ayrı kapsam.
    public class DailyReminderJob
    {
        private readonly MosaikContext _db;
        private readonly INotificationService _notifications;
        private readonly IEmailService _email;
        private readonly SmtpSettings _smtpSettings;
        private readonly ILogger<DailyReminderJob> _logger;

        public DailyReminderJob(
            MosaikContext db,
            INotificationService notifications,
            IEmailService email,
            IOptions<SmtpSettings> smtpOptions,
            ILogger<DailyReminderJob> logger)
        {
            _db = db;
            _notifications = notifications;
            _email = email;
            _smtpSettings = smtpOptions.Value;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            // Vade kıyası Türkiye yerel takvimine göre — 22:00 UTC sonrası UTC
            // "today" Turkey'de ertesi gün, vade kıyası yanlış olurdu.
            var today = BusinessClock.Today;
            _logger.LogDebug("DailyReminderJob: today (Turkey local) = {Today}", today);

            // Pending + henüz bildirim gönderilmemiş yükümlülükler.
            // ExecuteUpdateAsync ile direkt update yapılıyor — tracker gereksiz.
            var candidates = await _db.ContractObligations
                .AsNoTracking()
                .Where(o => o.Status == ObligationStatus.Pending && o.ReminderSentAt == null)
                .ToListAsync(ct);

            var toNotify = candidates.Where(o =>
                o.DueDate < today ||
                (o.ReminderDays.HasValue && (o.DueDate.DayNumber - today.DayNumber) <= o.ReminderDays.Value))
                .ToList();

            if (toNotify.Count == 0)
            {
                _logger.LogInformation("DailyReminderJob: bildirim gönderilecek yükümlülük yok.");
                return;
            }

            // Aktif kullanıcıları tek sorguda çek — FirmaIds CSV parse için in-memory filter.
            // Optimization: FirmaIds parse'i foreach DIŞINDA bir kez (N obligation × M user N+1 önlenir).
            var usersRaw = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.FirmaIds != null)
                .Select(u => new { u.UserId, u.FirmaIds, u.Email, u.Username })
                .ToListAsync(ct);

            var users = usersRaw
                .Select(u => new
                {
                    u.UserId,
                    u.Email,
                    u.Username,
                    FirmaIds = ParseFirmaIds(u.FirmaIds!)
                })
                .ToList();

            var firmas = await _db.Firmas.AsNoTracking()
                .Select(f => new { f.FirmaId, f.Name })
                .ToDictionaryAsync(f => f.FirmaId, f => f.Name, ct);

            int notifCount = 0;
            int emailCount = 0;
            int failedCount = 0;

            foreach (var obl in toNotify)
            {
                // Per-obligation izolasyon: tek bir yükümlülük patladığında diğerleri devam etsin
                // + ReminderSentAt o yükümlülüğe işlensin (batch'te tutarsak crash sonrası duplicate gönderim olur).
                try
                {
                    var isOverdue = obl.DueDate < today;
                    var daysLeft = obl.DueDate.DayNumber - today.DayNumber; // negatif = gecikmiş
                    var firmaName = firmas.GetValueOrDefault(obl.FirmaId, "—");
                    var dueDateStr = obl.DueDate.ToString("dd.MM.yyyy");

                    var targetUsers = users
                        .Where(u => u.FirmaIds.Contains(obl.FirmaId))
                        .ToList();

                    if (targetUsers.Count == 0)
                    {
                        _logger.LogDebug("DailyReminderJob: ObligationId={Id} için hedef kullanıcı yok (FirmaId={FirmaId})",
                            obl.Id, obl.FirmaId);
                        await MarkReminderSentAsync(obl.Id, ct);
                        continue;
                    }

                    var title = isOverdue
                        ? $"Gecikmiş yükümlülük: {obl.Title}"
                        : $"Hatırlatma: {obl.Title} ({daysLeft} gün kaldı)";

                    await _notifications.CreateBulkAsync(
                        targetUsers.Select(u => u.UserId),
                        entityType: "contract_obligation",
                        entityId: obl.Id,
                        title: title,
                        message: $"Son tarih: {dueDateStr} · Firma: {firmaName}",
                        targetUrl: "/Obligations",
                        notificationType: isOverdue ? "overdue" : "reminder",
                        createdBy: "system");

                    notifCount += targetUsers.Count;

                    if (_email.IsEnabled)
                    {
                        foreach (var u in targetUsers.Where(u => !string.IsNullOrWhiteSpace(u.Email)))
                        {
                            var body = isOverdue
                                ? EmailTemplates.OverdueObligation(obl.Title, dueDateStr, firmaName, _smtpSettings.AppUrl)
                                : EmailTemplates.ObligationReminder(obl.Title, dueDateStr, daysLeft, firmaName, _smtpSettings.AppUrl);

                            var subject = isOverdue
                                ? $"[Mosaik] Gecikmiş Yükümlülük: {obl.Title}"
                                : $"[Mosaik] Yükümlülük Hatırlatması: {obl.Title}";

                            var result = await _email.SendAsync(u.Email!, subject, body, ct);
                            if (result.IsSuccess)
                                emailCount++;
                            else if (!result.WasSkipped)
                                _logger.LogWarning(
                                    "DailyReminderJob: email gönderilemedi. User={User}, ObligationId={Id}, Reason={Reason}",
                                    u.Username, obl.Id, result.ErrorDetail);
                        }
                    }

                    await MarkReminderSentAsync(obl.Id, ct);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex,
                        "DailyReminderJob: ObligationId={Id} işlenirken hata; sonraki yükümlülükle devam ediliyor.",
                        obl.Id);
                    // ReminderSentAt set ETMİYORUZ — bir sonraki çalışmada tekrar denenecek.
                }
            }

            _logger.LogInformation(
                "DailyReminderJob tamamlandı. Toplam: {Total}, Bildirim: {Notif}, Email: {Email}, Hata: {Failed}",
                toNotify.Count, notifCount, emailCount, failedCount);
        }

        // Per-obligation ReminderSentAt update — partial failure durumunda batch ezilmesin.
        private async Task MarkReminderSentAsync(int obligationId, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            await _db.ContractObligations
                .Where(o => o.Id == obligationId)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.ReminderSentAt, now), ct);
        }

        private static IReadOnlySet<int> ParseFirmaIds(string csv)
        {
            var result = new HashSet<int>();
            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (int.TryParse(part, out var id))
                    result.Add(id);
            return result;
        }
    }
}
