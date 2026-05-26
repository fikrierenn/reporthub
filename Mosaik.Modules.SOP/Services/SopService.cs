using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34 Faz C — SOP master + version chain CRUD.
    // Yeni versiyon Draft olarak doğar; SopApprovalService.SubmitAsync ile Pending,
    // ApprovalRequest tüm step'leri Approved olunca Approved + önceki Approved versiyon SupersededDate set.
    public class SopService
    {
        private readonly DbContext _db;
        private readonly IAuditLog _audit;

        public SopService(DbContext db, IAuditLog audit)
        {
            _db = db;
            _audit = audit;
        }

        private DbSet<SopDocument> Documents => _db.Set<SopDocument>();
        private DbSet<SopVersion> Versions => _db.Set<SopVersion>();

        public Task<List<SopDocument>> ListByFirmaAsync(int firmaId, bool onlyActive = true) =>
            Documents.AsNoTracking()
                .Where(d => d.FirmaId == firmaId && (!onlyActive || d.IsActive))
                .OrderByDescending(d => d.UpdatedAt)
                .ToListAsync();

        public Task<SopDocument?> GetAsync(int id) =>
            Documents.AsNoTracking()
                .Include(d => d.Versions.OrderByDescending(v => v.VersionNumber))
                .FirstOrDefaultAsync(d => d.Id == id);

        public Task<SopVersion?> GetVersionAsync(int versionId) =>
            Versions.AsNoTracking()
                .Include(v => v.SopDocument)
                .FirstOrDefaultAsync(v => v.Id == versionId);

        public Task<SopVersion?> GetActiveVersionAsync(int sopDocumentId) =>
            Versions.AsNoTracking()
                .Where(v => v.SopDocumentId == sopDocumentId && v.Status == SopVersion.Approved)
                .OrderByDescending(v => v.EffectiveDate)
                .FirstOrDefaultAsync();

        public async Task<ServiceResult<SopDocument>> CreateAsync(SopDocumentInput input, int createdBy)
        {
            var validation = Validate(input);
            if (!validation.IsSuccess) return ServiceResult<SopDocument>.Failure(validation.Message);

            var entity = new SopDocument
            {
                FirmaId = input.FirmaId,
                Title = input.Title.Trim(),
                Description = input.Description,
                Category = input.Category,
                DepartmentIds = input.IsCompanyWide ? null : input.DepartmentIds,
                IsCompanyWide = input.IsCompanyWide,
                OwnerUserId = input.OwnerUserId,
                IsActive = true,
                ReadDeadlineDays = input.ReadDeadlineDays,
                RequiresIKApproval = input.RequiresIKApproval,
                AiAdvisorEnabled = input.AiAdvisorEnabled,
                DocumentNumber = input.DocumentNumber,
                RevisionNumber = input.RevisionNumber,
                PublishDate = input.PublishDate,
                RevisionDate = input.RevisionDate,
                EffectiveDate = input.EffectiveDate,
                PreparedBy = input.PreparedBy,
                ApprovedBy = input.ApprovedBy,
                ReviewFrequency = input.ReviewFrequency,
                Classification = input.Classification,
                CreatedBy = createdBy
            };

            Documents.Add(entity);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_created",
                targetType: "sop_document",
                targetKey: entity.Id.ToString(),
                description: $"SOP oluşturuldu: {entity.Title}");

            return ServiceResult<SopDocument>.Ok(entity, "SOP oluşturuldu.");
        }

        public async Task<ServiceResult<SopDocument>> UpdateAsync(int id, SopDocumentInput input, int updatedBy)
        {
            var entity = await Documents.FindAsync(id);
            if (entity == null) return ServiceResult<SopDocument>.Failure("SOP bulunamadı.");

            var validation = Validate(input);
            if (!validation.IsSuccess) return ServiceResult<SopDocument>.Failure(validation.Message);

            entity.Title = input.Title.Trim();
            entity.Description = input.Description;
            entity.Category = input.Category;
            entity.DepartmentIds = input.IsCompanyWide ? null : input.DepartmentIds;
            entity.IsCompanyWide = input.IsCompanyWide;
            entity.OwnerUserId = input.OwnerUserId;
            entity.ReadDeadlineDays = input.ReadDeadlineDays;
            entity.RequiresIKApproval = input.RequiresIKApproval;
            entity.AiAdvisorEnabled = input.AiAdvisorEnabled;
            entity.DocumentNumber = input.DocumentNumber;
            entity.RevisionNumber = input.RevisionNumber;
            entity.PublishDate = input.PublishDate;
            entity.RevisionDate = input.RevisionDate;
            entity.EffectiveDate = input.EffectiveDate;
            entity.PreparedBy = input.PreparedBy;
            entity.ApprovedBy = input.ApprovedBy;
            entity.ReviewFrequency = input.ReviewFrequency;
            entity.Classification = input.Classification;
            entity.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_updated",
                targetType: "sop_document",
                targetKey: entity.Id.ToString(),
                description: $"SOP güncellendi: {entity.Title}");

            return ServiceResult<SopDocument>.Ok(entity, "SOP güncellendi.");
        }

        public async Task<ServiceResult<SopVersion>> NewVersionAsync(int sopDocumentId, string contentJson, int createdBy)
        {
            var doc = await Documents.FindAsync(sopDocumentId);
            if (doc == null) return ServiceResult<SopVersion>.Failure("SOP bulunamadı.");
            if (string.IsNullOrWhiteSpace(contentJson))
                return ServiceResult<SopVersion>.Failure("İçerik boş olamaz.");

            var sanitized = SopContentSanitizer.Sanitize(contentJson);

            var nextNumber = await Versions
                .Where(v => v.SopDocumentId == sopDocumentId)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync() ?? 0;

            var version = new SopVersion
            {
                SopDocumentId = sopDocumentId,
                VersionNumber = nextNumber + 1,
                ContentJson = sanitized,
                PlainTextContent = SopContentSanitizer.ExtractPlainText(sanitized),
                Status = 0,                                              // Draft
                CreatedBy = createdBy
            };

            Versions.Add(version);
            doc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_version_created",
                targetType: "sop_version",
                targetKey: version.Id.ToString(),
                description: $"SOP v{version.VersionNumber} taslak oluşturuldu (Doc {sopDocumentId}).");

            return ServiceResult<SopVersion>.Ok(version, $"Versiyon {version.VersionNumber} oluşturuldu.");
        }

        // Plan 34.1: Soft delete — IsActive=false. Index listede gizlenir.
        // Hard delete istenirse ayrı endpoint (Approved versiyon varsa block).
        public async Task<ServiceResult> SoftDeleteAsync(int id, int deletedBy)
        {
            var entity = await Documents.FindAsync(id);
            if (entity == null) return ServiceResult.Failure("SOP bulunamadı.");
            if (!entity.IsActive) return ServiceResult.Ok("Zaten arşivlenmiş.");

            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_archived",
                targetType: "sop_document",
                targetKey: id.ToString(),
                description: $"SOP arşivlendi (soft delete): {entity.Title}");

            return ServiceResult.Ok("SOP arşivlendi.");
        }

        public async Task<ServiceResult> RestoreAsync(int id, int restoredBy)
        {
            var entity = await Documents.FindAsync(id);
            if (entity == null) return ServiceResult.Failure("SOP bulunamadı.");
            if (entity.IsActive) return ServiceResult.Ok("Zaten aktif.");

            entity.IsActive = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_restored",
                targetType: "sop_document",
                targetKey: id.ToString(),
                description: $"SOP geri yüklendi: {entity.Title}");

            return ServiceResult.Ok("SOP geri yüklendi.");
        }

        // Hard delete — sadece hiç Approved versiyonu olmayan SOP'lar için izin.
        // Approved versiyon = okundu kaydı + audit history, kayıp KVKK riski.
        public async Task<ServiceResult> HardDeleteAsync(int id, int deletedBy)
        {
            var entity = await Documents
                .Include(d => d.Versions)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (entity == null) return ServiceResult.Failure("SOP bulunamadı.");

            var hasApproved = entity.Versions.Any(v => v.Status == SopVersion.Approved || v.Status == SopVersion.Archived);
            if (hasApproved)
                return ServiceResult.Failure("Approved/Archived versiyonu olan SOP kalıcı silinemez. Arşivle.");

            var title = entity.Title;
            Documents.Remove(entity);
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_hard_deleted",
                targetType: "sop_document",
                targetKey: id.ToString(),
                description: $"SOP kalıcı silindi (Draft-only): {title}");

            return ServiceResult.Ok("SOP kalıcı silindi.");
        }

        // Plan 34.1: Draft versiyon içeriğini güncelle (TinyMCE editor save).
        // Sadece Status=Draft (0) versiyonlar düzenlenebilir — Pending/Approved/Archived değişmez.
        public async Task<ServiceResult<SopVersion>> UpdateVersionContentAsync(int versionId, string contentJson, int updatedBy)
        {
            var version = await Versions.FindAsync(versionId);
            if (version == null) return ServiceResult<SopVersion>.Failure("Versiyon bulunamadı.");
            if (version.Status != 0)
                return ServiceResult<SopVersion>.Failure("Sadece Draft versiyon düzenlenebilir.");
            if (string.IsNullOrWhiteSpace(contentJson))
                return ServiceResult<SopVersion>.Failure("İçerik boş olamaz.");

            var sanitized = SopContentSanitizer.Sanitize(contentJson);
            version.ContentJson = sanitized;
            version.PlainTextContent = SopContentSanitizer.ExtractPlainText(sanitized);
            // SopDocument.UpdatedAt'i de yenile (Index sıralaması için).
            var doc = await Documents.FindAsync(version.SopDocumentId);
            if (doc != null) doc.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_version_edited",
                targetType: "sop_version",
                targetKey: version.Id.ToString(),
                description: $"SOP v{version.VersionNumber} Draft içeriği güncellendi.");

            return ServiceResult<SopVersion>.Ok(version, "Versiyon güncellendi.");
        }

        // ApprovalRequest tüm step'leri Approved olunca SopApprovalService tarafından çağrılır.
        // - newVersion.Status = Approved + EffectiveDate set
        // - Önceki Approved versiyon → SupersededDate set (3 ay sonra Archived'a geçişi background job)
        public async Task<ServiceResult> MarkVersionApprovedAsync(int versionId)
        {
            var version = await Versions.FindAsync(versionId);
            if (version == null) return ServiceResult.Failure("Versiyon bulunamadı.");
            if (version.Status == SopVersion.Approved) return ServiceResult.Ok("Zaten Approved.");
            if (version.Status != SopVersion.Pending) return ServiceResult.Failure("Sadece Pending versiyon Approved olabilir.");

            var now = DateTime.UtcNow;
            version.Status = 2;
            version.EffectiveDate = now;

            var previousApproved = await Versions
                .Where(v => v.SopDocumentId == version.SopDocumentId
                         && v.Status == 2
                         && v.Id != version.Id
                         && v.SupersededDate == null)
                .ToListAsync();
            foreach (var prev in previousApproved)
                prev.SupersededDate = now;

            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_version_approved",
                targetType: "sop_version",
                targetKey: version.Id.ToString(),
                description: $"SOP v{version.VersionNumber} Approved (Doc {version.SopDocumentId}). Önceki {previousApproved.Count} versiyon superseded.");

            return ServiceResult.Ok("Versiyon onaylandı.");
        }

        // Periyodik temizlik: Superseded > 90 gün → Archived (Plan 34 §2.4 "3 ay sonra Status=Archived").
        // Hangfire Faz E'de çağıracak.
        public async Task<int> ArchiveSupersededAsync()
        {
            var threshold = DateTime.UtcNow.AddDays(-90);
            var staleVersions = await Versions
                .Where(v => v.Status == 2 && v.SupersededDate != null && v.SupersededDate < threshold)
                .ToListAsync();

            foreach (var v in staleVersions)
                v.Status = 3;                                            // Archived

            if (staleVersions.Count > 0)
            {
                await _db.SaveChangesAsync();
                await _audit.LogAsync(
                    eventType: "sop_archive_sweep",
                    targetType: "sop_version",
                    targetKey: "batch",
                    description: $"{staleVersions.Count} superseded versiyon arşivlendi (>90 gün).");
            }

            return staleVersions.Count;
        }

        private static ServiceResult Validate(SopDocumentInput input)
        {
            if (input.FirmaId <= 0) return ServiceResult.Failure("Firma seçimi zorunlu.");
            if (string.IsNullOrWhiteSpace(input.Title)) return ServiceResult.Failure("Başlık zorunlu.");
            if (input.Title.Length > 200) return ServiceResult.Failure("Başlık 200 karakteri aşamaz.");
            if (input.OwnerUserId <= 0) return ServiceResult.Failure("Sahip kullanıcı zorunlu.");
            if (input.ReadDeadlineDays < 1 || input.ReadDeadlineDays > 365)
                return ServiceResult.Failure("Okuma süresi 1-365 gün arası olmalı.");
            if (!input.IsCompanyWide && string.IsNullOrWhiteSpace(input.DepartmentIds))
                return ServiceResult.Failure("En az bir departman seç veya 'Şirket Geneli' işaretle.");
            return ServiceResult.Ok();
        }
    }

    public sealed record SopDocumentInput(
        int FirmaId,
        string Title,
        string? Description,
        string? Category,
        string? DepartmentIds,
        bool IsCompanyWide,
        int OwnerUserId,
        int ReadDeadlineDays,
        bool RequiresIKApproval,
        bool AiAdvisorEnabled,
        string? DocumentNumber = null,
        string? RevisionNumber = null,
        DateTime? PublishDate = null,
        DateTime? RevisionDate = null,
        DateTime? EffectiveDate = null,
        string? PreparedBy = null,
        string? ApprovedBy = null,
        string? ReviewFrequency = null,
        string? Classification = null);
}
