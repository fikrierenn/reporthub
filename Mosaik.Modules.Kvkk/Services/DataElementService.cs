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

        public Task<DataElement?> GetElementAsync(int id, CancellationToken ct = default) =>
            Elements.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && d.IsActive, ct);

        // Uyumluluk skoru (saf, test edilebilir): saklama kuralı tanımlı süreç oranı (%).
        // Yeni hesap motoru DEĞİL — mevcut zorunlu-alan doluluğundan türetilir (advisor).
        public static int CompliancePercent(int total, int withRetention) =>
            total <= 0 ? 0 : (int)Math.Round(withRetention * 100.0 / total);

        // Sorguya uyan veri öğelerini bul (code/displayName/alias — Türkçe-duyarsız).
        // NOT: in-memory matcher SADECE küçük/sınırlı katalog (~60 atomik öğe) için kabul edilebilir —
        // Türkçe İ/ı normalizasyonu SQL LIKE'a çevrilemediğinden. Büyük tabloda bu pattern'i kopyalama.
        public async Task<List<DataElement>> SearchElementsAsync(string query, CancellationToken ct = default)
        {
            var all = await Elements.AsNoTracking().Where(d => d.IsActive).ToListAsync(ct);
            return all.Where(d => DataElementMatcher.Matches(query, d.ElementCode, d.DisplayName, d.Aliases))
                .OrderBy(d => d.DisplayName)
                .ToList();
        }

        // Bir veri öğesinin işlendiği AKTİF süreçler (firma sınırlı). Reverse lookup hot path.
        // HasRetention/HasDisposal uyumluluk skoru + boşluk tespiti için.
        public async Task<List<ProcessUsage>> GetProcessesForElementAsync(
            int dataElementId, int firmaId, CancellationToken ct = default)
        {
            return await Links.AsNoTracking()
                .Where(l => l.DataElementId == dataElementId
                    && l.Process!.FirmaId == firmaId && l.Process.IsActive)
                .Select(l => new ProcessUsage(
                    l.ProcessId, l.Process!.Name, l.Process.Department, l.UsageType,
                    l.Process.RetentionRuleId != null || (l.Process.RetentionText != null && l.Process.RetentionText != ""),
                    l.Process.DisposalMethodId != null || (l.Process.DisposalText != null && l.Process.DisposalText != "")))
                .Distinct()
                .ToListAsync(ct);
        }

        public sealed record ProcessUsage(
            int ProcessId, string ProcessName, string Department, byte UsageType,
            bool HasRetention, bool HasDisposal);
    }
}
