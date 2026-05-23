using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Mosaik.Core.AI.Embed;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;

namespace Mosaik.Modules.SOP.Services
{
    // Plan 34.1 Faz 1 A-10 — SOP version Approved → chunks → embed → persist.
    // SopApprovalService.DispatchPublishedAsync sonrası Hangfire enqueue ile çağrılır.
    // Idempotent: aynı version için tekrar çalışırsa eski chunk'ları siler, yenilerini yazar.
    // Embedder hazır değilse (model dosyası eksik) audit warning + skip — publish bozulmaz.
    public class SopIndexer
    {
        private readonly DbContext _db;
        private readonly IMosaikEmbedder _embedder;
        private readonly IAuditLog _audit;
        private readonly ILogger<SopIndexer> _logger;

        public SopIndexer(
            DbContext db,
            IMosaikEmbedder embedder,
            IAuditLog audit,
            ILogger<SopIndexer> logger)
        {
            _db = db;
            _embedder = embedder;
            _audit = audit;
            _logger = logger;
        }

        public async Task<SopIndexResult> IndexVersionAsync(int versionId, CancellationToken ct = default)
        {
            if (!_embedder.IsReady)
            {
                _logger.LogWarning("SopIndexer: embedder hazır değil VersionId={VersionId}", versionId);
                await _audit.LogAsync(
                    eventType: "sop_index_skipped",
                    targetType: "sop_version",
                    targetKey: versionId.ToString(),
                    description: "Embedder hazır değil (e5-base ONNX dosyası eksik).",
                    isSuccess: false);
                return new SopIndexResult(versionId, 0, "embedder_not_ready");
            }

            var version = await _db.Set<SopVersion>()
                .FirstOrDefaultAsync(v => v.Id == versionId, ct);
            if (version is null)
            {
                _logger.LogWarning("SopIndexer: VersionId={VersionId} bulunamadı", versionId);
                return new SopIndexResult(versionId, 0, "version_not_found");
            }

            if (string.IsNullOrWhiteSpace(version.PlainTextContent))
            {
                _logger.LogInformation("SopIndexer: VersionId={VersionId} PlainTextContent boş, skip", versionId);
                return new SopIndexResult(versionId, 0, "empty_content");
            }

            // Idempotent: eski chunk'ları sil.
            var existing = await _db.Set<SopChunk>()
                .Where(c => c.SopVersionId == versionId)
                .ToListAsync(ct);
            if (existing.Count > 0)
            {
                _db.Set<SopChunk>().RemoveRange(existing);
                await _db.SaveChangesAsync(ct);
            }

            var chunks = SopChunker.Split(version.PlainTextContent);
            if (chunks.Count == 0)
            {
                _logger.LogInformation("SopIndexer: VersionId={VersionId} chunk üretmedi", versionId);
                return new SopIndexResult(versionId, 0, "no_chunks");
            }

            var embeddings = await _embedder.EmbedBatchAsync(chunks, EmbedRole.Passage, ct);

            for (int i = 0; i < chunks.Count; i++)
            {
                _db.Set<SopChunk>().Add(new SopChunk
                {
                    SopVersionId = versionId,
                    ChunkOrder = i,
                    Content = chunks[i],
                    EmbeddingJson = JsonSerializer.Serialize(embeddings[i]),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync(ct);

            await _audit.LogAsync(
                eventType: "sop_indexed",
                targetType: "sop_version",
                targetKey: versionId.ToString(),
                description: $"{chunks.Count} chunk üretildi, {embeddings.Length} embedding kaydedildi.");

            _logger.LogInformation(
                "SopIndexer: VersionId={VersionId} {Chunks} chunk indekslendi",
                versionId, chunks.Count);

            return new SopIndexResult(versionId, chunks.Count, "ok");
        }
    }

    public record SopIndexResult(int VersionId, int ChunkCount, string Status);
}
