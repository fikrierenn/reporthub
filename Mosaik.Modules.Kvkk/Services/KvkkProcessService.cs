using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Intelligence;
using Mosaik.Modules.Kvkk.Entities;

namespace Mosaik.Modules.Kvkk.Services
{
    // Plan 40 (M6) Faz 0 — KVKK süreç tanım CRUD + EntityRelations çift-yazma (Plan 38).
    // Modül DbContext base'i inject eder (_db.Set<T>), MosaikContext'e doğrudan bağımlı değil.
    public class KvkkProcessService
    {
        // Core whitelist sabitleri (Plan 38) — drift olmaması için tek kaynak.
        public const string ProcessEntityType = EntityType.KvkkProcess;
        public const string DataElementEntityType = EntityType.DataElement;
        public const string SopEntityType = EntityType.Sop;
        public const string ProcessesRelation = RelationType.Processes;
        public const string DerivedFromRelation = RelationType.DerivedFrom;

        private readonly DbContext _db;
        private readonly IEntityRelationService _relations;

        public KvkkProcessService(DbContext db, IEntityRelationService relations)
        {
            _db = db;
            _relations = relations;
        }

        private DbSet<KvkkProcess> Processes => _db.Set<KvkkProcess>();
        private DbSet<ProcessDataLink> Links => _db.Set<ProcessDataLink>();
        private DbSet<SopProcessLink> SopLinks => _db.Set<SopProcessLink>();

        public Task<List<KvkkProcess>> ListByFirmaAsync(int firmaId, CancellationToken ct = default) =>
            Processes.AsNoTracking()
                .Where(p => p.FirmaId == firmaId && p.IsActive)
                .OrderBy(p => p.Department).ThenBy(p => p.Name)
                .ToListAsync(ct);

        public Task<KvkkProcess?> GetAsync(int id, int firmaId, CancellationToken ct = default) =>
            Processes.AsNoTracking()
                .Include(p => p.LegalBasis)
                .Include(p => p.RetentionRule)
                .Include(p => p.DataLinks).ThenInclude(l => l.DataElement)
                .Include(p => p.CrossBorderTransfers)
                .FirstOrDefaultAsync(p => p.Id == id && p.FirmaId == firmaId, ct);

        public async Task<ServiceResult<int>> CreateAsync(KvkkProcess process, CancellationToken ct = default)
        {
            if (process.FirmaId <= 0)
                return ServiceResult<int>.Failure("Geçersiz firma.");
            if (string.IsNullOrWhiteSpace(process.Name))
                return ServiceResult<int>.Failure("Süreç adı boş olamaz.");
            if (string.IsNullOrWhiteSpace(process.Department))
                return ServiceResult<int>.Failure("Departman boş olamaz.");

            process.CreatedAt = DateTime.UtcNow;
            process.UpdatedAt = DateTime.UtcNow;
            Processes.Add(process);
            await _db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(process.Id);
        }

        // Process × DataElement bağla + EntityRelations'a çift-yaz (Plan 38 omurga).
        // Dönüş Data: yeni junction yaratıldı mı (true) yoksa zaten var mıydı (false) —
        // idempotent re-import'ta "kaç yeni bağ" doğru raporlanır.
        public async Task<ServiceResult<bool>> LinkDataElementAsync(
            int processId, int dataElementId, int firmaId, byte usageType = 0,
            string? notes = null, CancellationToken ct = default)
        {
            var owned = await Processes.AsNoTracking()
                .AnyAsync(p => p.Id == processId && p.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult<bool>.Failure("Süreç bulunamadı.");

            var exists = await Links.AnyAsync(
                l => l.ProcessId == processId && l.DataElementId == dataElementId && l.UsageType == usageType, ct);

            // Çift-yazma atomik: junction + polymorphic EntityRelation tek transaction'da
            // (ikisi de aynı scoped DbContext + ayrı SaveChanges → kısmi başarıyı önle).
            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            if (!exists)
            {
                Links.Add(new ProcessDataLink
                {
                    ProcessId = processId,
                    DataElementId = dataElementId,
                    UsageType = usageType,
                    Notes = notes
                });
                await _db.SaveChangesAsync(ct);
            }

            var rel = await _relations.AddAsync(new EntityRelationInput(
                FirmaId: firmaId,
                SourceType: ProcessEntityType,
                SourceId: processId,
                RelationType: ProcessesRelation,
                TargetType: DataElementEntityType,
                TargetId: dataElementId,
                SourceSystem: "kvkk"), ct);

            // Çift-yazma sözleşmesi: EntityRelation yazılamazsa junction'ı da geri al (atomik).
            if (!rel.IsSuccess)
                return ServiceResult<bool>.Failure("Veri öğesi ilişkilendirilemedi.");

            await tx.CommitAsync(ct);
            return ServiceResult<bool>.Ok(!exists);
        }

        // Plan 40 Faz 3 — Süreç adı/departman araması (SOP tarafındaki picker cross-area çağırır).
        // SQL Server default collation case-insensitive — StringComparison overload EF'e çevrilemez (code-reviewer bulgusu).
        public Task<List<KvkkProcess>> SearchAsync(string q, int firmaId, int take = 20, CancellationToken ct = default) =>
            Processes.AsNoTracking()
                .Where(p => p.FirmaId == firmaId && p.IsActive &&
                    (p.Name.Contains(q) || p.Department.Contains(q)))
                .OrderBy(p => p.Name)
                .Take(take)
                .ToListAsync(ct);

        public Task<List<SopProcessLink>> GetLinkedSopsAsync(int processId, int firmaId, CancellationToken ct = default) =>
            SopLinks.AsNoTracking()
                .Where(l => l.ProcessId == processId && l.FirmaId == firmaId)
                .OrderBy(l => l.SopTitle)
                .ToListAsync(ct);

        // Bir SOP'a bağlı tüm süreçler (SOP Details cross-area fetch bunu okur).
        public Task<List<(SopProcessLink Link, KvkkProcess Process)>> GetProcessesForSopAsync(
            int sopDocumentId, int firmaId, CancellationToken ct = default) =>
            SopLinks.AsNoTracking()
                .Where(l => l.SopDocumentId == sopDocumentId && l.FirmaId == firmaId)
                .Join(Processes, l => l.ProcessId, p => p.Id, (l, p) => new { l, p })
                .OrderBy(x => x.p.Name)
                .Select(x => new ValueTuple<SopProcessLink, KvkkProcess>(x.l, x.p))
                .ToListAsync(ct);

        // Cross-modül lookup — SopDocument CLR tipine referans yok (ADR-002), raw SQL ile aynı DB'den
        // FirmaId + Title okunur. security-reviewer H-2: client-supplied sopTitle + doğrulanmamış
        // sopDocumentId cross-firma referans/veri kirliliği riski taşıyordu — burada kapatıldı.
        private sealed record SopDocumentLookup(int FirmaId, string Title);

        // Junction + EntityRelations çift-yazma (LinkDataElementAsync ile aynı sözleşme).
        // sopTitle client'tan alınmaz — SOP tarafı DB'den doğrulanarak okunur (bkz. SopDocumentLookup).
        public async Task<ServiceResult<int>> LinkSopAsync(
            int processId, int sopDocumentId, int firmaId, CancellationToken ct = default)
        {
            var owned = await Processes.AsNoTracking().AnyAsync(p => p.Id == processId && p.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult<int>.Failure("Süreç bulunamadı.");

            var sop = await _db.Database.SqlQueryRaw<SopDocumentLookup>(
                    "SELECT FirmaId, Title FROM dbo.SopDocuments WHERE Id = {0}", sopDocumentId)
                .FirstOrDefaultAsync(ct);
            if (sop == null || sop.FirmaId != firmaId)
                return ServiceResult<int>.Failure("SOP bulunamadı.");

            var existing = await SopLinks.FirstOrDefaultAsync(
                l => l.ProcessId == processId && l.SopDocumentId == sopDocumentId, ct);
            if (existing != null)
                return ServiceResult<int>.Ok(existing.Id);

            await using var tx = await _db.Database.BeginTransactionAsync(ct);

            var link = new SopProcessLink
            {
                FirmaId = firmaId,
                ProcessId = processId,
                SopDocumentId = sopDocumentId,
                SopTitle = sop.Title
            };
            SopLinks.Add(link);
            await _db.SaveChangesAsync(ct);

            var rel = await _relations.AddAsync(new EntityRelationInput(
                FirmaId: firmaId,
                SourceType: SopEntityType,
                SourceId: sopDocumentId,
                RelationType: DerivedFromRelation,
                TargetType: ProcessEntityType,
                TargetId: processId,
                SourceSystem: "kvkk"), ct);

            if (!rel.IsSuccess)
                return ServiceResult<int>.Failure("SOP ilişkilendirilemedi.");

            await tx.CommitAsync(ct);
            return ServiceResult<int>.Ok(link.Id);
        }

        public async Task<ServiceResult<bool>> UnlinkSopAsync(int linkId, int firmaId, CancellationToken ct = default)
        {
            var link = await SopLinks.FirstOrDefaultAsync(l => l.Id == linkId && l.FirmaId == firmaId, ct);
            if (link == null)
                return ServiceResult<bool>.Failure("Bağlantı bulunamadı.");
            SopLinks.Remove(link);
            await _db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }
    }
}
