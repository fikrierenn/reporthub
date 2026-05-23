using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Domain;
using Mosaik.Core.Logging;
using Mosaik.Core.Notification;
using Mosaik.Core.Users;
using Mosaik.Core.Workflow;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34 §2.4 — SOP version → Mosaik.Core ApprovalRequest 3-adımlı flow (veya 2-adım).
    // Yazan → Departman Yöneticisi → İK Yetkilisi (RequiresIKApproval=false ise 3. adım atlanır).
    //
    // NOT: Mosaik.Modules.SOP sadece Mosaik.Core'a bağlı (ADR-002 modül izolasyonu).
    // Bu yüzden Mosaik.Services.ApprovalService inject edilmiyor — ApprovalRequest/Step
    // entity'lerine direkt DbContext üzerinden erişiyoruz. Mosaik.Services.ApprovalService
    // Mosaik.Core'a port edilirse ileride bu wrapper sadeleşir (Plan 16.5 follow-up).
    public class SopApprovalService
    {
        private readonly DbContext _db;
        private readonly SopService _sop;
        private readonly IAuditLog _audit;
        private readonly SopReadReceiptService _receipts;
        private readonly INotificationService _notifications;
        private readonly IActiveUserDirectory _userDirectory;
        private readonly SopIndexer _indexer;
        private readonly ILogger<SopApprovalService> _logger;

        // Plan 34 §2.4: rol bazlı adım. Onayçı UserId yerine ApproverRole.
        public const string DepartmentManagerRole = "department-manager";
        public const string IKApproverRole = "ik-approver";

        public SopApprovalService(
            DbContext db,
            SopService sop,
            IAuditLog audit,
            SopReadReceiptService receipts,
            INotificationService notifications,
            IActiveUserDirectory userDirectory,
            SopIndexer indexer,
            ILogger<SopApprovalService> logger)
        {
            _db = db;
            _sop = sop;
            _audit = audit;
            _receipts = receipts;
            _notifications = notifications;
            _userDirectory = userDirectory;
            _indexer = indexer;
            _logger = logger;
        }

        private DbSet<SopVersion> Versions => _db.Set<SopVersion>();
        private DbSet<SopApprovalSubmission> Submissions => _db.Set<SopApprovalSubmission>();
        private DbSet<ApprovalRequest> Requests => _db.Set<ApprovalRequest>();
        private DbSet<ApprovalStep> Steps => _db.Set<ApprovalStep>();

        // Submit: Draft versiyonu → Pending; ApprovalRequest oluştur; SopApprovalSubmission link.
        public async Task<ServiceResult<int>> SubmitAsync(int versionId, int submittedBy)
        {
            var version = await Versions
                .Include(v => v.SopDocument)
                .FirstOrDefaultAsync(v => v.Id == versionId);
            if (version == null) return ServiceResult<int>.Failure("Versiyon bulunamadı.");
            if (version.Status != 0) return ServiceResult<int>.Failure("Sadece Draft versiyon onaya gönderilebilir.");
            if (version.SopDocument == null) return ServiceResult<int>.Failure("Bağlı SOP bulunamadı.");

            var doc = version.SopDocument;

            var request = new ApprovalRequest
            {
                EntityType = "sop_version",
                EntityId = version.Id,
                Subject = $"SOP onayı: {doc.Title} v{version.VersionNumber}",
                Status = ApprovalStatus.Pending,
                CreatedBy = submittedBy.ToString()
            };
            request.Steps.Add(new ApprovalStep
            {
                StepOrder = 10,
                ApproverUserId = version.CreatedBy,                     // Yazan (otomatik onay sayılır)
                Status = ApprovalStatus.Pending,
                CreatedBy = submittedBy.ToString()
            });
            request.Steps.Add(new ApprovalStep
            {
                StepOrder = 20,
                ApproverRole = DepartmentManagerRole,
                Status = ApprovalStatus.Pending,
                CreatedBy = submittedBy.ToString()
            });
            if (doc.RequiresIKApproval)
            {
                request.Steps.Add(new ApprovalStep
                {
                    StepOrder = 30,
                    ApproverRole = IKApproverRole,
                    Status = ApprovalStatus.Pending,
                    CreatedBy = submittedBy.ToString()
                });
            }

            Requests.Add(request);
            Submissions.Add(new SopApprovalSubmission
            {
                SopVersionId = version.Id,
                ApprovalRequestId = 0,                                  // SaveChanges sonrası set edilir
                CreatedBy = submittedBy
            });
            version.Status = 1;                                          // Pending
            await _db.SaveChangesAsync();

            // SopApprovalSubmission.ApprovalRequestId güncelle (Request.Id artık var)
            var submission = await Submissions
                .Where(s => s.SopVersionId == version.Id && s.ApprovalRequestId == 0)
                .OrderByDescending(s => s.Id)
                .FirstAsync();
            submission.ApprovalRequestId = request.Id;
            await _db.SaveChangesAsync();

            await _audit.LogAsync(
                eventType: "sop_version_submitted",
                targetType: "sop_version",
                targetKey: version.Id.ToString(),
                description: $"SOP v{version.VersionNumber} onaya gönderildi (Request {request.Id}, {request.Steps.Count} adım).");

            return ServiceResult<int>.Ok(request.Id, "Onaya gönderildi.");
        }

        // Sıradaki Pending adım (MIN StepOrder).
        public Task<ApprovalStep?> GetCurrentStepAsync(int requestId) =>
            Steps.Where(s => s.RequestId == requestId && s.Status == ApprovalStatus.Pending)
                 .OrderBy(s => s.StepOrder)
                 .FirstOrDefaultAsync();

        public async Task<ServiceResult> DecideAsync(int stepId, ApprovalStatus decision, int decidingUserId, string? comment = null)
        {
            if (decision != ApprovalStatus.Approved && decision != ApprovalStatus.Rejected)
                return ServiceResult.Failure("Karar yalnızca Approved veya Rejected olabilir.");

            var step = await Steps.Include(s => s.Request).FirstOrDefaultAsync(s => s.Id == stepId);
            if (step == null) return ServiceResult.Failure("Adım bulunamadı.");
            if (step.Status != ApprovalStatus.Pending)
                return ServiceResult.Failure("Adım zaten karara bağlandı.");
            if (step.Request == null) return ServiceResult.Failure("İstek bulunamadı.");
            if (step.Request.EntityType != "sop_version")
                return ServiceResult.Failure("Bu adım SOP onayı değil.");

            // Sıralı kontrol: bu adım sıradaki MIN(StepOrder) Pending mi?
            var minPending = await Steps
                .Where(s => s.RequestId == step.RequestId && s.Status == ApprovalStatus.Pending)
                .MinAsync(s => (int?)s.StepOrder);
            if (minPending != step.StepOrder)
                return ServiceResult.Failure("Önceki adımlar kararlanmadan bu adım kararlanamaz.");

            step.Status = decision;
            step.DecidedAt = DateTime.UtcNow;
            step.Comment = comment;
            step.UpdatedBy = decidingUserId.ToString();
            step.UpdatedAt = DateTime.UtcNow;

            if (decision == ApprovalStatus.Rejected)
            {
                step.Request.Status = ApprovalStatus.Rejected;
                step.Request.CompletedAt = DateTime.UtcNow;
                var version = await Versions.FindAsync(step.Request.EntityId);
                if (version != null && version.Status == 1)
                {
                    version.Status = 0;                                  // Pending → Draft (yazar düzeltsin)
                }
                await _db.SaveChangesAsync();
                await _audit.LogAsync(
                    eventType: "sop_version_rejected",
                    targetType: "sop_version",
                    targetKey: step.Request.EntityId.ToString(),
                    description: $"SOP versiyon reddedildi (Request {step.Request.Id}, Step {step.StepOrder}).");
                return ServiceResult.Ok("Karar kaydedildi (reddedildi).");
            }

            // Approved — son adım mıydı?
            var pendingCount = await Steps
                .CountAsync(s => s.RequestId == step.RequestId
                              && s.Status == ApprovalStatus.Pending
                              && s.Id != stepId);
            if (pendingCount == 0)
            {
                step.Request.Status = ApprovalStatus.Approved;
                step.Request.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                await _sop.MarkVersionApprovedAsync(step.Request.EntityId);

                // S-20: yayınlanan versiyon → atanmış kullanıcı ataması + push bildirim.
                // Dispatch hatası onayı bozmaz (best-effort, audit log warning).
                await DispatchPublishedAsync(step.Request.EntityId);

                // Plan 34.1: RAG indexer — chunk + embed (AI Advisor için).
                // Embedder hazır değilse SopIndexer kendi içinde graceful skip.
                try { await _indexer.IndexVersionAsync(step.Request.EntityId); }
                catch (Exception ex) { _logger.LogWarning(ex, "SopIndexer hata VersionId={Id}", step.Request.EntityId); }
            }
            else
            {
                await _db.SaveChangesAsync();
            }

            return ServiceResult.Ok("Karar kaydedildi.");
        }

        public Task<List<SopApprovalSubmission>> ListByVersionAsync(int versionId) =>
            Submissions.AsNoTracking()
                .Where(s => s.SopVersionId == versionId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

        // Plan 34 Faz E S-20 — Approved SOP yayın akışı:
        // - IsCompanyWide=true: firma scope tüm aktif user → SopReadReceipt + INotificationService.
        // - IsCompanyWide=false (DepartmentIds CSV): Plan 18B HR sync sonrası department
        //   expansion eklenecek; şimdilik audit warning + skip (manuel atama UI ileride).
        // Hatalar publish'i bozmaz — audit'e best-effort yazılır.
        private async Task DispatchPublishedAsync(int versionId)
        {
            try
            {
                var version = await Versions
                    .Include(v => v.SopDocument)
                    .FirstOrDefaultAsync(v => v.Id == versionId);
                if (version?.SopDocument == null)
                {
                    _logger.LogWarning("SopApprovalService.DispatchPublishedAsync: VersionId={VersionId} bulunamadı, dispatch atlandı.", versionId);
                    return;
                }

                var doc = version.SopDocument;

                if (!doc.IsCompanyWide)
                {
                    await _audit.LogAsync(
                        eventType: "sop_dispatch_skipped",
                        targetType: "sop_version",
                        targetKey: version.Id.ToString(),
                        description: $"SOP v{version.VersionNumber} departman dağıtımı Plan 18B HR sync beklemede (DepartmentIds={doc.DepartmentIds ?? "(boş)"}).",
                        isSuccess: false);
                    return;
                }

                var userIds = await _userDirectory.GetActiveUserIdsAsync(doc.FirmaId);
                if (userIds.Count == 0)
                {
                    _logger.LogInformation("SopApprovalService: Firma {FirmaId} için aktif kullanıcı yok, dispatch boş.", doc.FirmaId);
                    return;
                }

                var assigned = await _receipts.AssignToUsersAsync(version.Id, userIds);

                var n = await _notifications.CreateBulkAsync(
                    userIds: userIds,
                    entityType: "sop_version",
                    entityId: version.Id,
                    title: $"Yeni prosedür: {doc.Title} v{version.VersionNumber}",
                    message: $"Yeni prosedür yayınlandı. Lütfen okuyup onaylayın ({doc.ReadDeadlineDays} gün içinde).",
                    targetUrl: $"/SOP/My/Read/{version.Id}",
                    notificationType: "SopPublished",
                    createdBy: "system");

                await _audit.LogAsync(
                    eventType: "sop_published",
                    targetType: "sop_version",
                    targetKey: version.Id.ToString(),
                    description: $"SOP v{version.VersionNumber} yayınlandı. {assigned} yeni atama, {n} bildirim gönderildi.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SopApprovalService.DispatchPublishedAsync hata VersionId={VersionId}. Yayın devam ediyor.",
                    versionId);
                try
                {
                    await _audit.LogAsync(
                        eventType: "sop_dispatch_failed",
                        targetType: "sop_version",
                        targetKey: versionId.ToString(),
                        description: $"SOP yayın bildirimi başarısız: {ex.GetType().Name}",
                        isSuccess: false);
                }
                catch
                {
                    // Audit log'da da hata olursa sessiz devam — publish kritik path değil.
                }
            }
        }
    }
}
