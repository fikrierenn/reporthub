using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 44 Faz 2 — SOP permission değişikliğinde chunk metadata senkronizasyonu.
    // SopController.Edit POST → BackgroundJob.Enqueue<ChunkPermissionSyncJob>(j => j.SyncAsync(id, "sop"))
    // Eventual consistency: 1-2 sn içinde tüm chunk'lara yansır.
    // Idempotent — tekrar çalışırsa sadece günceller.
    public class ChunkPermissionSyncJob
    {
        private readonly DbContext _db;
        private readonly ILogger<ChunkPermissionSyncJob> _logger;

        public ChunkPermissionSyncJob(DbContext db, ILogger<ChunkPermissionSyncJob> logger)
        {
            _db = db;
            _logger = logger;
        }

        // entityType: "sop" (gelecekteki RAG kaynakları için genişletilebilir)
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
