using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) Faz 5 — KvkkIntegrityFinding CRUD + idempotent upsert (dedup natural key
    // FirmaId+PatternCode+ProcessId). Dismiss state kalıcı — yeniden tespit sadece LastDetectedAt günceller.
    public class KvkkIntegrityFindingService(DbContext db)
    {
        private DbSet<KvkkIntegrityFinding> Findings => db.Set<KvkkIntegrityFinding>();

        // Var olan bulguyu günceller (dismiss durumu korunur) veya yeni satır açar.
        // Dönüş: (finding, isNew) — job bildirim gönderirken sadece yeni/yeniden-aktifleşen bulguyu kullanır.
        public async Task<(KvkkIntegrityFinding Finding, bool IsNew)> UpsertAsync(
            int firmaId, KvkkFindingCandidate candidate, CancellationToken ct = default)
        {
            var existing = await Findings.FirstOrDefaultAsync(f =>
                f.FirmaId == firmaId && f.PatternCode == candidate.PatternCode && f.ProcessId == candidate.ProcessId, ct);

            if (existing != null)
            {
                existing.LastDetectedAt = DateTime.UtcNow;
                existing.Description = candidate.Description;
                await db.SaveChangesAsync(ct);
                return (existing, false);
            }

            var finding = new KvkkIntegrityFinding
            {
                FirmaId = firmaId,
                PatternCode = candidate.PatternCode,
                Severity = candidate.Severity,
                ProcessId = candidate.ProcessId,
                Description = candidate.Description
            };
            Findings.Add(finding);
            await db.SaveChangesAsync(ct);
            return (finding, true);
        }

        public Task<List<KvkkIntegrityFinding>> ListAsync(int firmaId, bool includeDismissed = false, CancellationToken ct = default) =>
            Findings.AsNoTracking()
                .Where(f => f.FirmaId == firmaId && (includeDismissed || !f.IsDismissed))
                .Include(f => f.Process)
                .OrderByDescending(f => f.Severity).ThenByDescending(f => f.LastDetectedAt)
                .ToListAsync(ct);

        public async Task<ServiceResult<bool>> DismissAsync(int id, int firmaId, int userId, string? reason, CancellationToken ct = default)
        {
            var finding = await Findings.FirstOrDefaultAsync(f => f.Id == id && f.FirmaId == firmaId, ct);
            if (finding == null)
                return ServiceResult<bool>.Failure("Bulgu bulunamadı.");

            finding.IsDismissed = true;
            finding.DismissedBy = userId;
            finding.DismissedAt = DateTime.UtcNow;
            finding.DismissReason = reason;
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }
    }
}
