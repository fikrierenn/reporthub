using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Modules.Forms.Entities;

namespace Mosaik.Modules.Forms.Services
{
    public sealed record FormDefinitionInput(
        string Slug, string Name, string? Description, string? Category,
        bool IsPublic, bool IsAnonymous, bool IsEncrypted, int? TriggersWorkflowId = null);

    // Plan 41 Faz 2 — FormDefinition CRUD + Taslak→Yayında (snapshot) + Arşiv/Geri Yükle.
    // Plan 57 B3 — TriggersWorkflowId: submit'te tetiklenecek workflow şablonu (opsiyonel,
    // WorkflowTemplateLookupService ile fail-closed doğrulanır).
    public class FormDefinitionService(DbContext db, WorkflowTemplateLookupService workflowTemplates)
    {
        private DbSet<FormDefinition> Definitions => db.Set<FormDefinition>();

        public Task<List<FormDefinition>> ListByFirmaAsync(int firmaId, bool includeArchived = false, CancellationToken ct = default) =>
            Definitions.AsNoTracking()
                .Where(d => d.FirmaId == firmaId && (includeArchived || d.Status != 2))
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync(ct);

        public Task<FormDefinition?> GetAsync(int id, int firmaId, CancellationToken ct = default) =>
            Definitions.AsNoTracking()
                .Include(d => d.Fields.OrderBy(f => f.Order))
                .ThenInclude(f => f.DataElementMaps)
                .FirstOrDefaultAsync(d => d.Id == id && d.FirmaId == firmaId, ct);

        public async Task<ServiceResult<int>> CreateAsync(FormDefinitionInput input, int firmaId, int createdBy, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(input.Slug) || string.IsNullOrWhiteSpace(input.Name))
                return ServiceResult<int>.Failure("Slug ve ad zorunlu.");

            var slugExists = await Definitions.AsNoTracking()
                .AnyAsync(d => d.FirmaId == firmaId && d.Slug == input.Slug, ct);
            if (slugExists)
                return ServiceResult<int>.Failure("Bu slug zaten kullanılıyor.");

            if (input.TriggersWorkflowId is int twcId && !await workflowTemplates.ExistsAsync(twcId, firmaId, ct))
                return ServiceResult<int>.Failure("Seçilen onay şablonu bulunamadı veya bu form tipine uygun değil.");

            var def = new FormDefinition
            {
                FirmaId = firmaId,
                Slug = input.Slug.Trim(),
                Name = input.Name.Trim(),
                Description = input.Description,
                Category = input.Category,
                IsPublic = input.IsPublic,
                IsAnonymous = input.IsAnonymous,
                IsEncrypted = input.IsEncrypted,
                TriggersWorkflowId = input.TriggersWorkflowId,
                Status = 0,
                CreatedBy = createdBy
            };
            Definitions.Add(def);
            await db.SaveChangesAsync(ct);
            return ServiceResult<int>.Ok(def.Id);
        }

        public async Task<ServiceResult<bool>> UpdateAsync(int id, FormDefinitionInput input, int firmaId, CancellationToken ct = default)
        {
            var def = await Definitions.FirstOrDefaultAsync(d => d.Id == id && d.FirmaId == firmaId, ct);
            if (def == null)
                return ServiceResult<bool>.Failure("Form bulunamadı.");

            if (def.Slug != input.Slug)
            {
                var slugExists = await Definitions.AsNoTracking()
                    .AnyAsync(d => d.FirmaId == firmaId && d.Slug == input.Slug && d.Id != id, ct);
                if (slugExists)
                    return ServiceResult<bool>.Failure("Bu slug zaten kullanılıyor.");
            }

            if (input.TriggersWorkflowId is int twId && def.TriggersWorkflowId != twId
                && !await workflowTemplates.ExistsAsync(twId, firmaId, ct))
                return ServiceResult<bool>.Failure("Seçilen onay şablonu bulunamadı veya bu form tipine uygun değil.");

            def.Slug = input.Slug.Trim();
            def.Name = input.Name.Trim();
            def.Description = input.Description;
            def.Category = input.Category;
            def.IsPublic = input.IsPublic;
            def.IsAnonymous = input.IsAnonymous;
            def.IsEncrypted = input.IsEncrypted;
            def.TriggersWorkflowId = input.TriggersWorkflowId;
            def.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }

        // Taslak→Yayında: SchemaJson snapshot alınır (FormSchemaBuilder reuse), Version++.
        // DataElement mapping eksikse warning döner ama Yayında geçişini BLOKLAMAZ (§4.3).
        public async Task<ServiceResult<List<string>>> PublishAsync(int id, int firmaId, int publishedBy, CancellationToken ct = default)
        {
            var def = await Definitions
                .Include(d => d.Fields)
                .ThenInclude(f => f.DataElementMaps)
                .FirstOrDefaultAsync(d => d.Id == id && d.FirmaId == firmaId, ct);
            if (def == null)
                return ServiceResult<List<string>>.Failure("Form bulunamadı.");

            if (!FormPublishValidator.HasSubmittableField(def.Fields))
                return ServiceResult<List<string>>.Failure("Yayınlamak için en az bir veri alanı gerekli.");

            // silent-failure-hunter MEDIUM: seçenekli alan boş seçenekle yayınlanırsa canlıda
            // seçilemez dropdown çıkardı — publish-time blocker.
            var emptyChoiceErrors = FormPublishValidator.GetEmptyChoiceErrors(def.Fields);
            if (emptyChoiceErrors.Count > 0)
                return ServiceResult<List<string>>.Failure(string.Join(" ", emptyChoiceErrors));

            var mappedFieldIds = def.Fields.Where(f => f.DataElementMaps.Count > 0).Select(f => f.Id).ToHashSet();
            var warnings = FormPublishValidator.GetUnmappedFieldWarnings(def.Fields, mappedFieldIds);

            // MaxAsync(nullable) — boş sette null (DefaultIfEmpty EF SQL'e çevrilemez, code-reviewer/preview dersi).
            var nextVersion = (await db.Set<FormDefinitionVersion>().AsNoTracking()
                .Where(v => v.FormDefinitionId == id)
                .MaxAsync(v => (int?)v.Version, ct) ?? 0) + 1;

            var schemaJson = FormSchemaBuilder.BuildSchemaJson(def.Fields);
            db.Set<FormDefinitionVersion>().Add(new FormDefinitionVersion
            {
                FormDefinitionId = id,
                Version = nextVersion,
                SchemaJson = schemaJson,
                PublishedBy = publishedBy
            });

            def.Status = 1;
            def.Version = nextVersion;
            def.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);

            return ServiceResult<List<string>>.Ok(warnings, warnings.Count > 0
                ? $"Yayınlandı — {warnings.Count} alan KVKK'ya bağlı değil (opsiyonel)."
                : "Yayınlandı.");
        }

        public async Task<ServiceResult<bool>> SetStatusAsync(int id, int firmaId, byte status, CancellationToken ct = default)
        {
            var def = await Definitions.FirstOrDefaultAsync(d => d.Id == id && d.FirmaId == firmaId, ct);
            if (def == null)
                return ServiceResult<bool>.Failure("Form bulunamadı.");
            def.Status = status;
            def.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return ServiceResult<bool>.Ok(true);
        }
    }
}
