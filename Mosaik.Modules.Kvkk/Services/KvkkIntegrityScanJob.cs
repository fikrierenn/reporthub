using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Notification;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) Faz 5 — Hangfire günlük tarama. Her firma için 7 pattern (Pattern 4 ertelendi)
    // çalıştırır, bulguları upsert eder; yeni Kritik/İdari-Yüksek bulguyu süreç sahibine bildirir
    // (INotificationService → mevcut NotificationDigestJob toplar, ayrı SMTP path açılmaz — advisor kararı).
    public class KvkkIntegrityScanJob(
        DbContext db,
        KvkkIntegrityChecker checker,
        KvkkIntegrityFindingService findings,
        INotificationService notifications,
        ILogger<KvkkIntegrityScanJob> logger)
    {
        private const byte NotifyMinSeverity = 1; // İdari-Yüksek ve üstü

        public async Task ExecuteAsync(CancellationToken ct = default)
        {
            var firmaIds = await db.Set<KvkkProcess>().AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => p.FirmaId)
                .Distinct()
                .ToListAsync(ct);

            int totalNew = 0, totalUpdated = 0, notified = 0, notifySkipped = 0, failedFirmas = 0;

            foreach (var firmaId in firmaIds)
            {
                try
                {
                    var candidates = await checker.ScanAsync(firmaId, ct);
                    foreach (var candidate in candidates)
                    {
                        var (finding, isNew) = await findings.UpsertAsync(firmaId, candidate, ct);
                        if (isNew) totalNew++; else totalUpdated++;

                        if (isNew && !finding.IsDismissed && finding.Severity >= NotifyMinSeverity)
                        {
                            var sent = await NotifyOwnerAsync(finding, ct);
                            if (sent) notified++; else notifySkipped++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    failedFirmas++;
                    logger.LogError(ex, "KvkkIntegrityScanJob: firma {FirmaId} taranamadı, atlanıyor.", firmaId);
                }
            }

            logger.LogInformation(
                "KvkkIntegrityScanJob tamamlandı. Firma: {FirmaCount} (Hatalı: {Failed}), Yeni: {New}, Güncellenen: {Updated}, Bildirim: {Notified} (Atlanan: {Skipped})",
                firmaIds.Count, failedFirmas, totalNew, totalUpdated, notified, notifySkipped);
        }

        // Dönüş: bildirim gönderildi mi. Skip (firma-seviyesi bulgu / sahipsiz süreç) sessiz değil —
        // yüksek şiddetli bulguda hedefsiz kalma LogWarning ile görünür (silent-failure-hunter bulgusu).
        private async Task<bool> NotifyOwnerAsync(KvkkIntegrityFinding finding, CancellationToken ct)
        {
            if (finding.ProcessId == null)
            {
                logger.LogWarning(
                    "KVKK bulgu {FindingId} (şiddet {Severity}) firma-seviyesi — bildirim hedefi yok, sadece dashboard'da görünür.",
                    finding.Id, finding.Severity);
                return false;
            }

            var ownerId = await db.Set<KvkkProcess>().AsNoTracking()
                .Where(p => p.Id == finding.ProcessId)
                .Select(p => p.LastReviewedBy)
                .FirstOrDefaultAsync(ct);
            if (ownerId is null)
            {
                logger.LogWarning(
                    "KVKK bulgu {FindingId} (şiddet {Severity}, süreç {ProcessId}) incelenmemiş — bildirim atlandı, sadece dashboard'da görünür.",
                    finding.Id, finding.Severity, finding.ProcessId);
                return false;
            }

            await notifications.CreateAsync(
                ownerId.Value, "kvkk_finding", finding.Id,
                "KVKK bütünlük bulgusu", finding.Description,
                "/Kvkk/IntegrityFinding", "warning", "system");
            return true;
        }
    }
}
