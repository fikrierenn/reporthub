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
        public const string ProcessesRelation = RelationType.Processes;

        private readonly DbContext _db;
        private readonly IEntityRelationService _relations;

        public KvkkProcessService(DbContext db, IEntityRelationService relations)
        {
            _db = db;
            _relations = relations;
        }

        private DbSet<KvkkProcess> Processes => _db.Set<KvkkProcess>();
        private DbSet<ProcessDataLink> Links => _db.Set<ProcessDataLink>();

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
        public async Task<ServiceResult> LinkDataElementAsync(
            int processId, int dataElementId, int firmaId, byte usageType = 0,
            string? notes = null, CancellationToken ct = default)
        {
            var owned = await Processes.AsNoTracking()
                .AnyAsync(p => p.Id == processId && p.FirmaId == firmaId, ct);
            if (!owned)
                return ServiceResult.Failure("Süreç bulunamadı.");

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
                return ServiceResult.Failure("Veri öğesi ilişkilendirilemedi.");

            await tx.CommitAsync(ct);
            return ServiceResult.Ok();
        }
    }
}
