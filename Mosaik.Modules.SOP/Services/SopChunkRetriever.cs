using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 1 A-11 — Cosine similarity retrieval.
    // L2 normalize edilmiş embedding'ler için cosine = dot product.
    // In-memory: tüm chunk'ları çek + dot hesapla + top-K filter.
    // Şu an küçük corpus (<50 SOP × ~10 chunk = 500 vector) için yeterli.
    // İleride SQL Server 2025 VECTOR kolon refactor (Plan 27 Faz E shared kit).
    public class SopChunkRetriever
    {
        private readonly DbContext _db;
        private readonly ILogger<SopChunkRetriever> _logger;

        public SopChunkRetriever(DbContext db, ILogger<SopChunkRetriever> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Approved versionların chunk'ları arasında top-K en yakını.
        // minScore: bu eşiğin altındaki chunk'lar reddedilir (RAG kalite filtresi).
        // firmaId verilirse o firma scope.
        public async Task<List<SopChunkHit>> SearchAsync(
            float[] queryEmbedding,
            int topK = 4,
            double minScore = 0.5,
            int? firmaId = null,
            CancellationToken ct = default)
        {
            if (queryEmbedding.Length == 0)
            {
                _logger.LogWarning("SopChunkRetriever: boş query embedding");
                return new();
            }

            // Sadece Approved + IsActive SOP'ların chunk'ları.
            var query = _db.Set<SopChunk>()
                .AsNoTracking()
                .Where(c => c.SopVersion != null
                         && c.SopVersion.Status == 2                       // Approved
                         && c.SopVersion.SopDocument != null
                         && c.SopVersion.SopDocument.IsActive);
            if (firmaId is not null and not 0)
                query = query.Where(c => c.SopVersion!.SopDocument!.FirmaId == firmaId.Value);

            var rows = await query
                .Select(c => new
                {
                    c.Id,
                    c.SopVersionId,
                    c.ChunkOrder,
                    c.Content,
                    c.EmbeddingJson,
                    DocId = c.SopVersion!.SopDocumentId,
                    DocTitle = c.SopVersion.SopDocument!.Title,
                    VersionNumber = c.SopVersion.VersionNumber
                })
                .ToListAsync(ct);

            var hits = new List<SopChunkHit>(rows.Count);
            foreach (var r in rows)
            {
                ct.ThrowIfCancellationRequested();
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

            return hits
                .OrderByDescending(h => h.Score)
                .Take(topK)
                .ToList();
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
