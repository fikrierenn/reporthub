using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Email;
using Mosaik.Core.Messaging;
using Mosaik.Models;
using Mosaik.Services.Email;

namespace Mosaik.Services
{
    // C-01 (Plan 33 Faz 2) — Günlük hatırlatma cron (09:00).
    // Kapsamı:
    //   1. Pending ContractObligations: DueDate - bugün <= ReminderDays → hatırlatma bildirimi
    //   2. Pending ContractObligations: DueDate < bugün → gecikme bildirimi
    //   3. Her ikisi için: IMessenger (in-app + email birleşik)
    //   4. ReminderSentAt = now() → aynı yükümlülük için tekrar gönderim olmaz
    // Calendar modülü (Plan 22) ve Tamim hatırlatması (C-03) ayrı kapsam.
    public class DailyReminderJob
    {
        private readonly MosaikContext _db;
        private readonly IMessenger _messenger;
        private readonly SmtpSettings _smtpSettings;
        private readonly IBusinessClock _clock;
        private readonly ILogger<DailyReminderJob> _logger;

        public DailyReminderJob(
            MosaikContext db,
            IMessenger messenger,
            Microsoft.Extensions.Options.IOptions<SmtpSettings> smtpOptions,
            IBusinessClock clock,
            ILogger<DailyReminderJob> logger)
        {
            _db = db;
            _messenger = messenger;
            _smtpSettings = smtpOptions.Value;
            _clock = clock;
            _logger = logger;
        }

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            // Vade kıyası Türkiye yerel takvimine göre — 22:00 UTC sonrası UTC
            // "today" Turkey'de ertesi gün, vade kıyası yanlış olurdu.
            var today = _clock.Today;
            _logger.LogDebug("DailyReminderJob: today (Turkey local) = {Today}", today);

            // Pending + henüz bildirim gönderilmemiş yükümlülükler.
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

            // Aktif kullanıcıları tek sorguda çek.
            var usersRaw = await _db.Users.AsNoTracking()
                .Where(u => u.IsActive && u.FirmaIds != null)
                .Select(u => new { u.UserId, u.FirmaIds })
                .ToListAsync(ct);

            var users = usersRaw
                .Select(u => new
                {
                    u.UserId,
                    FirmaIds = ParseFirmaIds(u.FirmaIds!)
                })
                .ToList();

            var firmas = await _db.Firmas.AsNoTracking()
                .Select(f => new { f.FirmaId, f.Name })
                .ToDictionaryAsync(f => f.FirmaId, f => f.Name, ct);

            int sentCount = 0;
            int failedCount = 0;

            foreach (var obl in toNotify)
            {
                try
                {
                    var isOverdue = obl.DueDate < today;
                    var daysLeft = obl.DueDate.DayNumber - today.DayNumber;
                    var firmaName = firmas.GetValueOrDefault(obl.FirmaId, "—");
                    var dueDateStr = obl.DueDate.ToString("dd.MM.yyyy");

                    var targetUsers = users
                        .Where(u => u.FirmaIds.Contains(obl.FirmaId))
                        .Select(u => u.UserId)
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

                    var htmlEmail = isOverdue
                        ? EmailTemplates.OverdueObligation(obl.Title, dueDateStr, firmaName, _smtpSettings.AppUrl)
                        : EmailTemplates.ObligationReminder(obl.Title, dueDateStr, daysLeft, firmaName, _smtpSettings.AppUrl);

                    var externalKey = $"obligation_{(isOverdue ? "overdue" : "reminder")}:{obl.Id}:{today:yyyyMMdd}";

                    await _messenger.SendBulkAsync(targetUsers, new MessengerPayload(
                        Title: title,
                        Body: $"Son tarih: {dueDateStr} · Firma: {firmaName}",
                        HtmlEmail: htmlEmail,
                        EntityType: "contract_obligation",
                        EntityId: obl.Id,
                        TargetUrl: "/Obligations",
                        NotificationType: isOverdue ? "overdue" : "reminder",
                        ExternalKey: externalKey
                    ), ct);

                    sentCount += targetUsers.Count;
                    await MarkReminderSentAsync(obl.Id, ct);
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogError(ex,
                        "DailyReminderJob: ObligationId={Id} işlenirken hata; sonraki yükümlülükle devam ediliyor.",
                        obl.Id);
                }
            }

            _logger.LogInformation(
                "DailyReminderJob tamamlandı. Toplam: {Total}, Gönderilen: {Sent}, Hata: {Failed}",
                toNotify.Count, sentCount, failedCount);
        }

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
