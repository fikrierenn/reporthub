using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Rag;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 1 A-11 + Plan 44 Faz 2 — Cosine similarity retrieval w/ permission guard.
    // L2 normalize edilmiş embedding'ler için cosine = dot product.
    // In-memory: tüm eligible chunk'ları çek + dot hesapla + top-K filter.
    // Plan 44: 2 savunma katmanı:
    //   1) SQL WHERE SecurityLevel <= userClearance (kaba filtre, index kullanır)
    //   2) IRagAccessPolicy.IsAccessible (role/dept/user whitelist in-memory — kesin engel)
    public class SopChunkRetriever
    {
        private readonly DbContext _db;
        private readonly IRagAccessPolicy _policy;
        private readonly ILogger<SopChunkRetriever> _logger;

        public SopChunkRetriever(DbContext db, IRagAccessPolicy policy, ILogger<SopChunkRetriever> logger)
        {
            _db = db;
            _policy = policy;
            _logger = logger;
        }

        // RagUserContext ile çağrılan ana overload (Plan 44).
        public async Task<List<SopChunkHit>> SearchAsync(
            float[] queryEmbedding,
            RagUserContext user,
            int topK = 4,
            double minScore = 0.5,
            CancellationToken ct = default)
        {
            if (queryEmbedding.Length == 0)
            {
                _logger.LogWarning("SopChunkRetriever: boş query embedding");
                return new();
            }

            // Katman 1 — SQL: Approved + Active + FirmaId + SecurityLevel <= clearance
            var query = _db.Set<SopChunk>()
                .AsNoTracking()
                .Where(c => c.SopVersion != null
                         && c.SopVersion.Status == SopVersion.Approved
                         && c.SopVersion.SopDocument != null
                         && c.SopVersion.SopDocument.IsActive
                         && (user.FirmaId == 0 || c.SopVersion.SopDocument.FirmaId == user.FirmaId)
                         && c.SecurityLevel <= user.SecurityClearance);

            var rows = await query
                .Select(c => new
                {
                    c.Id,
                    c.SopVersionId,
                    c.ChunkOrder,
                    c.Content,
                    c.EmbeddingJson,
                    c.SecurityLevel,
                    c.AllowedRoleIds,
                    c.AllowedDepartmentIds,
                    c.AllowedUserIds,
                    DocId = c.SopVersion!.SopDocumentId,
                    DocTitle = c.SopVersion.SopDocument!.Title,
                    VersionNumber = c.SopVersion.VersionNumber
                })
                .ToListAsync(ct);

            var hits = new List<SopChunkHit>(rows.Count);
            int blocked = 0;
            foreach (var r in rows)
            {
                ct.ThrowIfCancellationRequested();

                // Katman 2 — in-memory: role/dept/user whitelist (defense-in-depth)
                if (!_policy.IsAccessible(r.SecurityLevel, r.AllowedRoleIds, r.AllowedDepartmentIds, r.AllowedUserIds, user))
                {
                    blocked++;
                    continue;
                }

                var emb = JsonSerializer.Deserialize<float[]>(r.EmbeddingJson);
                if (emb is null || emb.Length != queryEmbedding.Length) continue;

                double score = DotProduct(queryEmbedding, emb);
                if (score < minScore) continue;

                hits.Add(new SopChunkHit(
                    ChunkId: r.Id,
                    SopVersionId: r.SopVersionId,
                    SopDocumentId: r.DocId,
                    SopTitle: r.DocTitle,
                    VersionNumber: r.VersionNumber,
                    ChunkOrder: r.ChunkOrder,
                    Content: r.Content,
                    Score: score));
            }

            if (blocked > 0)
                _logger.LogInformation(
                    "SopChunkRetriever: {Blocked} chunk erişim engellendi (UserId={UserId}, FirmaId={FirmaId})",
                    blocked, user.UserId, user.FirmaId);

            return hits
                .OrderByDescending(h => h.Score)
                .Take(topK)
                .ToList();
        }

        // Geriye uyumluluk overload — admin (clearance=3) + tüm roller.
        // DI gerektirmeyen test/seed senaryoları için.
        public Task<List<SopChunkHit>> SearchAsync(
            float[] queryEmbedding,
            int topK = 4,
            double minScore = 0.5,
            int? firmaId = null,
            CancellationToken ct = default)
        {
            var ctx = new RagUserContext(
                UserId: 0,
                FirmaId: firmaId ?? 0,
                SecurityClearance: SopChunk.Restricted,
                RoleNames: new[] { "admin" },
                DepartmentIds: Array.Empty<int>());
            return SearchAsync(queryEmbedding, ctx, topK, minScore, ct);
        }

        private static double DotProduct(float[] a, float[] b)
        {
            double sum = 0;
            int len = Math.Min(a.Length, b.Length);
            for (int i = 0; i < len; i++) sum += a[i] * b[i];
            return sum;
        }
    }

    public record SopChunkHit(
        int ChunkId,
        int SopVersionId,
        int SopDocumentId,
        string SopTitle,
        int VersionNumber,
        int ChunkOrder,
        string Content,
        double Score);
}
