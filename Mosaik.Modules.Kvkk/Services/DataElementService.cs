using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) Faz 0 — reverse navigation çekirdeği: "bu veri nerede işleniyor?".
    public class DataElementService
    {
        private readonly DbContext _db;

        public DataElementService(DbContext db) => _db = db;

        private DbSet<DataElement> Elements => _db.Set<DataElement>();
        private DbSet<ProcessDataLink> Links => _db.Set<ProcessDataLink>();
        private DbSet<KvkkProcess> Processes => _db.Set<KvkkProcess>();

        public Task<List<DataElement>> ListAsync(CancellationToken ct = default) =>
            Elements.AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.DisplayName)
                .ToListAsync(ct);

        // Sorguya uyan veri öğelerini bul (code/displayName/alias — Türkçe-duyarsız).
        public async Task<List<DataElement>> SearchElementsAsync(string query, CancellationToken ct = default)
        {
            var all = await Elements.AsNoTracking().Where(d => d.IsActive).ToListAsync(ct);
            return all.Where(d => DataElementMatcher.Matches(query, d.ElementCode, d.DisplayName, d.Aliases))
                .OrderBy(d => d.DisplayName)
                .ToList();
        }

        // Bir veri öğesinin işlendiği süreçler (firma sınırlı). Reverse lookup hot path.
        public async Task<List<ProcessUsage>> GetProcessesForElementAsync(
            int dataElementId, int firmaId, CancellationToken ct = default)
        {
            return await Links.AsNoTracking()
                .Where(l => l.DataElementId == dataElementId && l.Process!.FirmaId == firmaId)
                .Select(l => new ProcessUsage(
                    l.ProcessId, l.Process!.Name, l.Process.Department, l.UsageType))
                .Distinct()
                .ToListAsync(ct);
        }

        public sealed record ProcessUsage(int ProcessId, string ProcessName, string Department, byte UsageType);
    }
}
