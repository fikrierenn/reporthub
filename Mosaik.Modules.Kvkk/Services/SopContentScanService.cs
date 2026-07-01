using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) Faz 3 — SOP düz-metin içeriğini DataElement alias'larıyla eşleştirir + persist eder.
    // SopDocument entity'sine hiç dokunmaz (girdi=string, cross-modül referans yok — ADR-002).
    public class SopContentScanService(DbContext db)
    {
        private DbSet<SopScannedElement> Scanned => db.Set<SopScannedElement>();

        // Bir SOP'un içeriğini tarar, önceki tarama sonucunu değiştirir (delete+insert — idempotent).
        public async Task<List<DataElement>> ScanAsync(int sopDocumentId, string plainText, int firmaId, CancellationToken ct = default)
        {
            var elements = await db.Set<DataElement>().AsNoTracking().Where(e => e.IsActive).ToListAsync(ct);
            var matches = elements
                .Where(e => DataElementMatcher.TextContainsElement(plainText, e.DisplayName, e.Aliases))
                .ToList();

            var old = await Scanned.Where(s => s.SopDocumentId == sopDocumentId).ToListAsync(ct);
            Scanned.RemoveRange(old);
            Scanned.AddRange(matches.Select(e => new SopScannedElement
            {
                FirmaId = firmaId,
                SopDocumentId = sopDocumentId,
                DataElementId = e.Id
            }));
            await db.SaveChangesAsync(ct);

            return matches;
        }

        // Bir sürece bağlı tüm SOP'ların taradığı veri öğeleri, ama sürecin ProcessDataLink'inde
        // olmayanlar — "öneri panel" (Pattern 8 integrity check ile aynı fark mantığı).
        public async Task<List<DataElement>> GetSuggestedDataElementsAsync(int processId, CancellationToken ct = default)
        {
            var sopIds = await db.Set<SopProcessLink>().AsNoTracking()
                .Where(l => l.ProcessId == processId)
                .Select(l => l.SopDocumentId)
                .ToListAsync(ct);
            if (sopIds.Count == 0)
                return [];

            var scannedElementIds = await Scanned.AsNoTracking()
                .Where(s => sopIds.Contains(s.SopDocumentId))
                .Select(s => s.DataElementId)
                .Distinct()
                .ToListAsync(ct);
            if (scannedElementIds.Count == 0)
                return [];

            var linkedElementIds = await db.Set<ProcessDataLink>().AsNoTracking()
                .Where(l => l.ProcessId == processId)
                .Select(l => l.DataElementId)
                .ToListAsync(ct);

            var suggestedIds = scannedElementIds.Except(linkedElementIds).ToList();
            if (suggestedIds.Count == 0)
                return [];

            return await db.Set<DataElement>().AsNoTracking()
                .Where(e => suggestedIds.Contains(e.Id))
                .OrderBy(e => e.DisplayName)
                .ToListAsync(ct);
        }
    }
}
