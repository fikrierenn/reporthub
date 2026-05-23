using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 6 A-28 — KVKK retention: SopAiConversation 1-yıl üstü silinir.
    // Audit log ana kaydı (AuditLogs tablosu) 5-yıl ayrı tutulur (cross-modül).
    // Hangfire RecurringJob "sop-ai-history-cleanup" daily 03:00 Europe/Istanbul.
    public class SopAiHistoryCleanupJob
    {
        private static readonly TimeSpan RetentionWindow = TimeSpan.FromDays(365);

        private readonly DbContext _db;
        private readonly IAuditLog _audit;
        private readonly ILogger<SopAiHistoryCleanupJob> _logger;

        public SopAiHistoryCleanupJob(DbContext db, IAuditLog audit, ILogger<SopAiHistoryCleanupJob> logger)
        {
            _db = db;
            _audit = audit;
            _logger = logger;
        }

        public async Task<int> ExecuteAsync(CancellationToken ct = default)
        {
            var cutoff = DateTime.UtcNow - RetentionWindow;

            // RemoveRange pattern (InMemory + SqlServer ortak). Production SqlServer'da
            // ExecuteDeleteAsync daha verimli olur ama 1-yıl üstü kayıt sayısı sınırlı
            // (yıllık ~365 × N user × 20/saat << milyon) — load + delete kabul edilir.
            var stale = await _db.Set<SopAiConversation>()
                .Where(c => c.CreatedAt < cutoff)
                .ToListAsync(ct);

            var deleted = stale.Count;
            if (deleted > 0)
            {
                _db.Set<SopAiConversation>().RemoveRange(stale);
                await _db.SaveChangesAsync(ct);
            }

            _logger.LogInformation(
                "SopAiHistoryCleanupJob: {Deleted} eski conversation silindi (cutoff {Cutoff:yyyy-MM-dd}).",
                deleted, cutoff);

            if (deleted > 0)
            {
                await _audit.LogAsync(
                    eventType: "sop_ai_history_purged",
                    targetType: "sop_ai_conversation",
                    targetKey: cutoff.ToString("yyyyMMdd"),
                    description: $"KVKK retention: {deleted} conversation silindi (>1 yıl).");
            }

            return deleted;
        }
    }
}
