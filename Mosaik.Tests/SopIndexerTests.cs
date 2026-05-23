using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.AI.Embed;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Faz 1 A-10 — SopIndexer: chunk + embed + persist, idempotent, graceful skip.
public class SopIndexerTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }
        public DbSet<SopDocument> SopDocuments => Set<SopDocument>();
        public DbSet<SopVersion> SopVersions => Set<SopVersion>();
        public DbSet<SopChunk> SopChunks => Set<SopChunk>();

        protected override void OnModelCreating(ModelBuilder mb)
            => new Mosaik.Modules.SOP.SopModule().ConfigureModelBuilder(mb);
    }

    private sealed class FakeAudit : IAuditLog
    {
        public List<string> Events { get; } = new();
        public Task LogAsync(string eventType, string? targetType = null, string? targetKey = null,
            string? description = null, string? oldValuesJson = null, string? newValuesJson = null, bool isSuccess = true)
        {
            Events.Add(eventType);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeEmbedder : IMosaikEmbedder
    {
        public int Dimensions => 3;
        public bool IsReady { get; set; } = true;
        public int BatchCallCount { get; private set; }

        public Task<float[]> EmbedAsync(string text, EmbedRole role, CancellationToken ct = default)
            => Task.FromResult(new float[] { 1, 0, 0 });

        public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, EmbedRole role, CancellationToken ct = default)
        {
            BatchCallCount++;
            return Task.FromResult(texts.Select(_ => new float[] { 1, 0, 0 }).ToArray());
        }
    }

    private static (TestContext db, SopIndexer svc, FakeAudit audit, FakeEmbedder emb) NewService(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var emb = new FakeEmbedder();
        var svc = new SopIndexer(db, emb, audit, NullLogger<SopIndexer>.Instance);
        return (db, svc, audit, emb);
    }

    private static async Task<SopVersion> SeedVersion(TestContext db, string? plainText = "Bu bir test prosedürüdür.")
    {
        var doc = new SopDocument { FirmaId = 1, Title = "T", OwnerUserId = 10, IsActive = true, CreatedBy = 10 };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();
        var v = new SopVersion
        {
            SopDocumentId = doc.Id,
            VersionNumber = 1,
            ContentJson = "<p>x</p>",
            PlainTextContent = plainText,
            Status = 2,
            CreatedBy = 10
        };
        db.SopVersions.Add(v);
        await db.SaveChangesAsync();
        return v;
    }

    [Fact]
    public async Task Index_EmbedderNotReady_GracefulSkip()
    {
        var (db, svc, audit, emb) = NewService(nameof(Index_EmbedderNotReady_GracefulSkip));
        await using var _ = db;
        var v = await SeedVersion(db);
        emb.IsReady = false;

        var result = await svc.IndexVersionAsync(v.Id);

        Assert.Equal("embedder_not_ready", result.Status);
        Assert.Equal(0, result.ChunkCount);
        Assert.Contains("sop_index_skipped", audit.Events);
    }

    [Fact]
    public async Task Index_VersionNotFound_Fails()
    {
        var (db, svc, _, _) = NewService(nameof(Index_VersionNotFound_Fails));
        await using var _ = db;

        var result = await svc.IndexVersionAsync(versionId: 9999);

        Assert.Equal("version_not_found", result.Status);
    }

    [Fact]
    public async Task Index_EmptyPlainText_Skipped()
    {
        var (db, svc, _, _) = NewService(nameof(Index_EmptyPlainText_Skipped));
        await using var _ = db;
        var v = await SeedVersion(db, plainText: "");

        var result = await svc.IndexVersionAsync(v.Id);

        Assert.Equal("empty_content", result.Status);
        Assert.Equal(0, await db.SopChunks.CountAsync());
    }

    [Fact]
    public async Task Index_Success_PersistsChunksAndEmbeddings()
    {
        var (db, svc, audit, emb) = NewService(nameof(Index_Success_PersistsChunksAndEmbeddings));
        await using var _ = db;
        var v = await SeedVersion(db, plainText: "Şirket içi prosedür içeriği. İlk paragraf metni.");

        var result = await svc.IndexVersionAsync(v.Id);

        Assert.Equal("ok", result.Status);
        Assert.True(result.ChunkCount >= 1);

        var chunks = await db.SopChunks.Where(c => c.SopVersionId == v.Id).ToListAsync();
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c =>
        {
            Assert.False(string.IsNullOrEmpty(c.EmbeddingJson));
            var vec = JsonSerializer.Deserialize<float[]>(c.EmbeddingJson);
            Assert.NotNull(vec);
            Assert.Equal(3, vec!.Length);                                       // FakeEmbedder dim 3
        });
        Assert.Equal(1, emb.BatchCallCount);
        Assert.Contains("sop_indexed", audit.Events);
    }

    [Fact]
    public async Task Index_Idempotent_DeletesOldChunksFirst()
    {
        var (db, svc, _, _) = NewService(nameof(Index_Idempotent_DeletesOldChunksFirst));
        await using var _ = db;
        var v = await SeedVersion(db, plainText: "İlk indeksleme metni.");

        // İlk indeksleme + stale chunk yarat (farklı içerik simülasyonu)
        await svc.IndexVersionAsync(v.Id);
        var initialCount = await db.SopChunks.CountAsync(c => c.SopVersionId == v.Id);
        Assert.True(initialCount > 0);

        // İkinci çağrı — eski chunk'lar silinmeli, yenisi yazılmalı
        var result = await svc.IndexVersionAsync(v.Id);

        Assert.Equal("ok", result.Status);
        var finalCount = await db.SopChunks.CountAsync(c => c.SopVersionId == v.Id);
        Assert.Equal(result.ChunkCount, finalCount);                            // duplicate yok
    }
}
