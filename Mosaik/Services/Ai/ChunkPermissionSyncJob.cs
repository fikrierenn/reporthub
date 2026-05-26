using Microsoft.EntityFrameworkCore;
using Mosaik.Models;

namespace Mosaik.Services.Ai
{
    // Plan 44 Faz 2 — SOP/Document permission değişikliğinde chunk metadata senkronizasyonu.
    // SopController.Edit POST → BackgroundJob.Enqueue<ChunkPermissionSyncJob>(j => j.SyncAsync(id, "sop"))
    // Eventual consistency: 1-2 sn içinde tüm chunk'lara yansır.
    // Idempotent — tekrar çalışırsa sadece günceller.
    public class ChunkPermissionSyncJob
    {
        private readonly MosaikContext _db;
        private readonly ILogger<ChunkPermissionSyncJob> _logger;

        public ChunkPermissionSyncJob(MosaikContext db, ILogger<ChunkPermissionSyncJob> logger)
        {
            _db = db;
            _logger = logger;
        }

        // entityType: "sop" | "document" | "contract" (gelecekteki RAG kaynakları için genişletilebilir)
        public async Task SyncAsync(int entityId, string entityType, CancellationToken ct = default)
        {
            switch (entityType.ToLowerInvariant())
            {
                case "sop":
                    await SyncSopChunksAsync(entityId, ct);
                    break;
                default:
                    _logger.LogWarning(
                        "ChunkPermissionSyncJob: bilinmeyen entityType={Type}, sync atlandı.",
                        entityType);
                    break;
            }
        }

        private async Task SyncSopChunksAsync(int sopDocumentId, CancellationToken ct)
        {
            // SopDocument permission'larını çek (SOP modül DbContext değil — Mosaik.Core DbContext üzerinden).
            // Plan 44: SopDocument'ta SecurityLevel + AllowedRoleIds + AllowedDepartmentIds + AllowedUserIds
            // şu an yok (Admin UI Faz 3'te). Şimdilik SopChunks → Internal default ile sync.
            // Faz 3 sonrası: SopDocument'tan oku, chunk'lara yaz.
            var affected = await _db.Database.ExecuteSqlRawAsync(
                """
                UPDATE sc
                SET    sc.SecurityLevel        = ISNULL(sd.SecurityLevel, 1),
                       sc.AllowedRoleIds       = sd.AllowedRoleIds,
                       sc.AllowedDepartmentIds = sd.AllowedDepartmentIds,
                       sc.AllowedUserIds       = sd.AllowedUserIds
                FROM   dbo.SopChunks    sc
                JOIN   dbo.SopVersions  sv ON sv.Id = sc.SopVersionId
                JOIN   dbo.SopDocuments sd ON sd.Id = sv.SopDocumentId
                WHERE  sd.Id = {0}
                """,
                sopDocumentId);

            _logger.LogInformation(
                "ChunkPermissionSyncJob: SOP {Id} için {Count} chunk güncellendi.",
                sopDocumentId, affected);
        }
    }
}
