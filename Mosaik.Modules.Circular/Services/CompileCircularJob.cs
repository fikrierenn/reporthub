using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Logging;
using Mosaik.Modules.Circular.Models;

namespace Mosaik.Modules.Circular.Services
{
    // Plan 17 Faz D — Günlük tamim derleme cron job (17:00).
    // Bugünün tarih aralığında IsActive=true VE CircularId IS NULL olan blocks'u
    // (henüz tamime girmemiş) tek Circular zarfı altında topla.
    //
    // Davranış:
    //  - Bugün için zaten Circular varsa: yeni gelen pending blocks o zarfa eklenir
    //  - Hiç pending block yoksa: hiçbir şey yapma (boş tamim üretme)
    //  - PublishedAt set edilir, CircularNumber TAM-YYYYMMDD
    //
    // Manuel tetikleme: AdminController.CompileNow (test için)
    public class CompileCircularJob
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;
        private readonly CircularSummaryService _summary;
        private readonly ILogger<CompileCircularJob> _logger;

        public CompileCircularJob(
            DbContext db,
            IAuditLog audit,
            CircularSummaryService summary,
            ILogger<CompileCircularJob> logger)
        {
            _db = db;
            _audit = audit;
            _summary = summary;
            _logger = logger;
        }

        public async Task<CompileResult> ExecuteAsync(DateTime? forDate = null)
        {
            var date = (forDate ?? DateTime.UtcNow.Date).Date;

            var pendingBlocks = await _db.Set<DailyBlock>()
                .Where(b => b.IsActive && b.CircularId == null && b.BlockDate == date)
                .OrderByDescending(b => b.IsUrgent)
                .ThenBy(b => b.Id)
                .ToListAsync();

            if (pendingBlocks.Count == 0)
            {
                await _audit.LogAsync(
                    eventType: "circular_compile_skipped",
                    targetType: "circular",
                    targetKey: date.ToString("yyyyMMdd"),
                    description: $"Compile çalıştı ama {date:yyyy-MM-dd} için pending block yok.");
                return new CompileResult(false, 0, null, "Pending block yok, tamim oluşturulmadı.");
            }

            // Bugün için zaten bir Circular var mı?
            var circular = await _db.Set<Models.Circular>()
                .FirstOrDefaultAsync(c => c.CircularDate == date);

            var isNew = circular == null;
            if (isNew)
            {
                circular = new Models.Circular
                {
                    CircularNumber = $"TAM-{date:yyyyMMdd}",
                    Title = $"{date:dd MMMM yyyy} — Günlük Tamim",
                    CircularDate = date,
                    PublishedAt = DateTime.UtcNow,
                };
                _db.Set<Models.Circular>().Add(circular);
                await _db.SaveChangesAsync();
            }

            // Blokları zarfa bağla
            foreach (var block in pendingBlocks)
            {
                block.CircularId = circular!.Id;
            }
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "circular_published",
                targetType: "circular",
                targetKey: circular!.Id.ToString(),
                description: $"{circular.CircularNumber}: {pendingBlocks.Count} block eklendi (yeni={isNew}).");

            // Plan 17 Faz F — AI özet üret (best-effort, hata olursa tamim yine yayında)
            try
            {
                await _summary.GenerateAsync(circular.Id);
            }
            catch (Exception ex)
            {
                // AI özet hatası publish'i bozmaz — log'la, devam et.
                _logger.LogWarning(ex,
                    "CompileCircularJob: AI summary generation failed for CircularId={CircularId}. Publish devam ediyor.",
                    circular.Id);
            }

            return new CompileResult(true, pendingBlocks.Count, circular.Id, isNew
                ? $"{circular.CircularNumber} oluşturuldu, {pendingBlocks.Count} block eklendi."
                : $"{circular.CircularNumber}'a {pendingBlocks.Count} block eklendi.");
        }
    }

    public record CompileResult(bool Success, int BlockCount, int? CircularId, string Message);
}
