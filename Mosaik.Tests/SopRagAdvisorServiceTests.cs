using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.AI.Embed;
using Mosaik.Core.AI.Local;
using Mosaik.Core.AI.Skills;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Faz 2 A-14 — RAG advisor davranış: hazır değil → graceful, hit yok → fallback,
// hit var → LLM çağrı + persist + audit + sources dedup.
public class SopRagAdvisorServiceTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbSet<SopDocument> SopDocuments => Set<SopDocument>();
        public DbSet<SopVersion> SopVersions => Set<SopVersion>();
        public DbSet<SopChunk> SopChunks => Set<SopChunk>();
        public DbSet<SopAiConversation> SopAiConversations => Set<SopAiConversation>();
        public DbSet<SopReadReceipt> SopReadReceipts => Set<SopReadReceipt>();
        public DbSet<SopApprovalSubmission> SopApprovalSubmissions => Set<SopApprovalSubmission>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            new Mosaik.Modules.SOP.SopModule().ConfigureModelBuilder(mb);
        }
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
        public float[] FixedEmbedding { get; set; } = Normalize(new float[] { 1, 0, 0 });

        public Task<float[]> EmbedAsync(string text, EmbedRole role, CancellationToken ct = default)
            => Task.FromResult(FixedEmbedding);

        public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, EmbedRole role, CancellationToken ct = default)
            => Task.FromResult(texts.Select(_ => FixedEmbedding).ToArray());

        public static float[] Normalize(float[] v)
        {
            double n = 0;
            foreach (var f in v) n += f * f;
            n = Math.Sqrt(n);
            if (n == 0) return v;
            var s = (float)(1.0 / n);
            return v.Select(f => f * s).ToArray();
        }
    }

    private sealed class EmptySkillCatalog : ISkillCatalog
    {
        public Task<IReadOnlyList<SkillManifest>> ListAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SkillManifest>>(Array.Empty<SkillManifest>());
        public Task<Skill?> GetAsync(string id, CancellationToken ct = default)
            => Task.FromResult<Skill?>(null);
        public Task<IReadOnlyList<SkillManifest>> MatchAsync(SkillMatchContext ctx, int top = 3, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<SkillManifest>>(Array.Empty<SkillManifest>());
        public void Reload() { }
    }

    private sealed class FakeLlm : ILlmRunner
    {
        public bool IsReady { get; set; } = true;
        public string Answer { get; set; } = "Test cevap. Kaynaklara göre prosedür şöyle.";
        public bool ShouldFail { get; set; }
        public int CallCount { get; private set; }

        public Task<LlmRunResult> RunAsync(string systemPrompt, string userPrompt, LlmRunOptions options, CancellationToken ct = default)
        {
            CallCount++;
            if (ShouldFail) return Task.FromResult(new LlmRunResult(false, null, 0, 0, "Mock fail"));
            return Task.FromResult(new LlmRunResult(true, Answer, 100, 50));
        }
    }

    private static (TestContext db, SopRagAdvisorService svc, FakeAudit audit, FakeEmbedder emb, FakeLlm llm) NewService(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var emb = new FakeEmbedder();
        var llm = new FakeLlm();
        var retriever = new SopChunkRetriever(db, NullLogger<SopChunkRetriever>.Instance);
        var rateLimit = new SopRateLimitGuard(db);
        var skills = new EmptySkillCatalog();
        var svc = new SopRagAdvisorService(db, emb, llm, retriever, rateLimit, skills, audit, NullLogger<SopRagAdvisorService>.Instance);
        return (db, svc, audit, emb, llm);
    }

    private static async Task SeedChunkAsync(TestContext db, int firmaId, string title, float[] emb)
    {
        var doc = new SopDocument
        {
            FirmaId = firmaId,
            Title = title,
            OwnerUserId = 10,
            IsActive = true,
            CreatedBy = 10
        };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();

        var version = new SopVersion
        {
            SopDocumentId = doc.Id,
            VersionNumber = 1,
            ContentJson = "<p>x</p>",
            PlainTextContent = $"İçerik: {title}",
            Status = 2,
            CreatedBy = 10
        };
        db.SopVersions.Add(version);
        await db.SaveChangesAsync();

        db.SopChunks.Add(new SopChunk
        {
            SopVersionId = version.Id,
            ChunkOrder = 0,
            Content = $"Chunk içeriği: {title}",
            EmbeddingJson = JsonSerializer.Serialize(FakeEmbedder.Normalize(emb))
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Ask_EmptyQuestion_ReturnsFailure()
    {
        var (db, svc, _, _, _) = NewService(nameof(Ask_EmptyQuestion_ReturnsFailure));
        await using var _ = db;

        var result = await svc.AskAsync(userId: 1, firmaId: 1, isAdmin: false, question: "  ");

        Assert.False(result.IsSuccess);
        Assert.Contains("boş", result.Message);
    }

    [Fact]
    public async Task Ask_EmbedderNotReady_ReturnsFailure()
    {
        var (db, svc, _, emb, _) = NewService(nameof(Ask_EmbedderNotReady_ReturnsFailure));
        await using var _ = db;

        emb.IsReady = false;
        var result = await svc.AskAsync(userId: 1, firmaId: 1, isAdmin: false, question: "İzin nasıl alınır?");

        Assert.False(result.IsSuccess);
        Assert.Contains("AI", result.Message);
    }

    [Fact]
    public async Task Ask_LlmNotReady_ReturnsFailure()
    {
        var (db, svc, _, _, llm) = NewService(nameof(Ask_LlmNotReady_ReturnsFailure));
        await using var _ = db;

        llm.IsReady = false;
        var result = await svc.AskAsync(userId: 1, firmaId: 1, isAdmin: false, question: "İzin nasıl alınır?");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Ask_NoHits_PersistsAndReturnsFallback()
    {
        var (db, svc, audit, emb, llm) = NewService(nameof(Ask_NoHits_PersistsAndReturnsFallback));
        await using var _ = db;

        // Hit yok — DB'de hiç chunk yok.
        var result = await svc.AskAsync(userId: 99, firmaId: 1, isAdmin: false, question: "Yıllık izin maksimum kaç gün?");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.NoHits);
        Assert.Empty(result.Data.Sources);
        Assert.Contains("bulunamadı", result.Data.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, llm.CallCount);                                    // LLM çağrılmadı

        // Persist + audit
        var conversations = await db.SopAiConversations.ToListAsync();
        Assert.Single(conversations);
        Assert.Null(conversations[0].SourceSopVersionIds);
        Assert.Contains("sop_ai_question_asked", audit.Events);
    }

    [Fact]
    public async Task Ask_WithHits_CallsLlmAndPersists()
    {
        var (db, svc, audit, emb, llm) = NewService(nameof(Ask_WithHits_CallsLlmAndPersists));
        await using var _ = db;

        await SeedChunkAsync(db, firmaId: 1, title: "Yıllık İzin SOP", emb: new float[] { 1, 0, 0 });
        emb.FixedEmbedding = FakeEmbedder.Normalize(new float[] { 1, 0, 0 });
        llm.Answer = "Yıllık izin 14 gündür.";

        var result = await svc.AskAsync(userId: 99, firmaId: 1, isAdmin: false, question: "Yıllık izin maksimum kaç gün?");

        Assert.True(result.IsSuccess);
        Assert.False(result.Data!.NoHits);
        Assert.Equal("Yıllık izin 14 gündür.", result.Data.Answer);
        Assert.Single(result.Data.Sources);
        Assert.Equal("Yıllık İzin SOP", result.Data.Sources[0].Title);
        Assert.Equal(1, llm.CallCount);

        // Persist: SourceSopVersionIds dolu
        var conv = await db.SopAiConversations.FirstAsync();
        Assert.NotNull(conv.SourceSopVersionIds);
        Assert.Equal(100, conv.TokensIn);
        Assert.Equal(50, conv.TokensOut);
        Assert.Contains("sop_ai_question_asked", audit.Events);
    }

    [Fact]
    public async Task Ask_LlmFails_ReturnsFailureWithoutPersist()
    {
        var (db, svc, _, _, llm) = NewService(nameof(Ask_LlmFails_ReturnsFailureWithoutPersist));
        await using var _ = db;

        await SeedChunkAsync(db, firmaId: 1, title: "X", emb: new float[] { 1, 0, 0 });
        llm.ShouldFail = true;

        var result = await svc.AskAsync(userId: 1, firmaId: 1, isAdmin: false, question: "Soru");

        Assert.False(result.IsSuccess);
        Assert.Empty(await db.SopAiConversations.ToListAsync());
    }

    [Fact]
    public async Task Ask_DuplicateSops_DeduplicatedInSources()
    {
        var (db, svc, _, emb, _) = NewService(nameof(Ask_DuplicateSops_DeduplicatedInSources));
        await using var _ = db;

        // Aynı SOP'tan 2 chunk + farklı SOP 1 chunk.
        var doc1 = new SopDocument { FirmaId = 1, Title = "SOP-A", OwnerUserId = 10, IsActive = true, CreatedBy = 10 };
        var doc2 = new SopDocument { FirmaId = 1, Title = "SOP-B", OwnerUserId = 10, IsActive = true, CreatedBy = 10 };
        db.SopDocuments.AddRange(doc1, doc2);
        await db.SaveChangesAsync();

        var v1 = new SopVersion { SopDocumentId = doc1.Id, VersionNumber = 1, ContentJson = "x", Status = 2, CreatedBy = 10 };
        var v2 = new SopVersion { SopDocumentId = doc2.Id, VersionNumber = 1, ContentJson = "y", Status = 2, CreatedBy = 10 };
        db.SopVersions.AddRange(v1, v2);
        await db.SaveChangesAsync();

        var emb1 = JsonSerializer.Serialize(FakeEmbedder.Normalize(new float[] { 1, 0, 0 }));
        db.SopChunks.AddRange(
            new SopChunk { SopVersionId = v1.Id, ChunkOrder = 0, Content = "A1", EmbeddingJson = emb1 },
            new SopChunk { SopVersionId = v1.Id, ChunkOrder = 1, Content = "A2", EmbeddingJson = emb1 },
            new SopChunk { SopVersionId = v2.Id, ChunkOrder = 0, Content = "B1", EmbeddingJson = emb1 });
        await db.SaveChangesAsync();

        var result = await svc.AskAsync(userId: 1, firmaId: 1, isAdmin: false, question: "X");

        Assert.True(result.IsSuccess);
        // 3 chunk hit ama 2 unique SOP
        Assert.Equal(2, result.Data!.Sources.Count);
        Assert.Contains(result.Data.Sources, s => s.Title == "SOP-A");
        Assert.Contains(result.Data.Sources, s => s.Title == "SOP-B");
    }

    [Fact]
    public async Task Ask_RateLimitExceeded_NonAdmin_Rejected()
    {
        var (db, svc, _, _, llm) = NewService(nameof(Ask_RateLimitExceeded_NonAdmin_Rejected));
        await using var _ = db;

        // 20 conversation pencere içinde — limit dolu.
        var now = DateTime.UtcNow;
        for (int i = 0; i < SopRateLimitGuard.HourlyLimit; i++)
        {
            db.SopAiConversations.Add(new SopAiConversation
            {
                UserId = 7,
                Question = $"q{i}",
                Answer = "a",
                CreatedAt = now.AddMinutes(-30).AddSeconds(i)
            });
        }
        await db.SaveChangesAsync();

        var result = await svc.AskAsync(userId: 7, firmaId: 1, isAdmin: false, question: "Yeni soru");

        Assert.False(result.IsSuccess);
        Assert.Contains("sınır", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, llm.CallCount);
    }

    [Fact]
    public async Task SubmitFeedback_OwnConversation_UpdatesFeedback()
    {
        var (db, svc, audit, _, _) = NewService(nameof(SubmitFeedback_OwnConversation_UpdatesFeedback));
        await using var _ = db;

        var conv = new SopAiConversation { UserId = 5, Question = "q", Answer = "a", CreatedAt = DateTime.UtcNow };
        db.SopAiConversations.Add(conv);
        await db.SaveChangesAsync();

        var result = await svc.SubmitFeedbackAsync(conv.Id, userId: 5, feedback: (byte)2, "yararsız");

        Assert.True(result.IsSuccess);
        var refreshed = await db.SopAiConversations.FindAsync(conv.Id);
        Assert.Equal((byte)2, refreshed!.UserFeedback);
        Assert.Equal("yararsız", refreshed.FeedbackNote);
        Assert.NotNull(refreshed.FeedbackAt);
        Assert.Contains("sop_ai_feedback_down", audit.Events);
    }

    [Fact]
    public async Task SubmitFeedback_OtherUserConversation_Rejected()
    {
        var (db, svc, _, _, _) = NewService(nameof(SubmitFeedback_OtherUserConversation_Rejected));
        await using var _ = db;

        var conv = new SopAiConversation { UserId = 5, Question = "q", Answer = "a", CreatedAt = DateTime.UtcNow };
        db.SopAiConversations.Add(conv);
        await db.SaveChangesAsync();

        // userId=99 başkasının kaydına feedback veremez.
        var result = await svc.SubmitFeedbackAsync(conv.Id, userId: 99, feedback: (byte)1, null);

        Assert.False(result.IsSuccess);
        var refreshed = await db.SopAiConversations.FindAsync(conv.Id);
        Assert.Equal((byte)0, refreshed!.UserFeedback);
    }

    [Fact]
    public async Task SubmitFeedback_NonexistentConversation_Failure()
    {
        var (db, svc, _, _, _) = NewService(nameof(SubmitFeedback_NonexistentConversation_Failure));
        await using var _ = db;

        var result = await svc.SubmitFeedbackAsync(conversationId: 999, userId: 1, feedback: (byte)1, null);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SubmitFeedback_InvalidFeedbackValue_Failure()
    {
        var (db, svc, _, _, _) = NewService(nameof(SubmitFeedback_InvalidFeedbackValue_Failure));
        await using var _ = db;

        var conv = new SopAiConversation { UserId = 1, Question = "q", Answer = "a", CreatedAt = DateTime.UtcNow };
        db.SopAiConversations.Add(conv);
        await db.SaveChangesAsync();

        var result = await svc.SubmitFeedbackAsync(conv.Id, 1, feedback: (byte)0, null);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Ask_RateLimitExceeded_Admin_Bypasses()
    {
        var (db, svc, _, _, llm) = NewService(nameof(Ask_RateLimitExceeded_Admin_Bypasses));
        await using var _ = db;

        var now = DateTime.UtcNow;
        for (int i = 0; i < SopRateLimitGuard.HourlyLimit + 10; i++)
        {
            db.SopAiConversations.Add(new SopAiConversation
            {
                UserId = 7,
                Question = $"q{i}",
                Answer = "a",
                CreatedAt = now.AddMinutes(-30).AddSeconds(i)
            });
        }
        await db.SaveChangesAsync();

        // Admin → bypass. Hit yok → NoHits fallback, LLM yine çağrılmaz ama rate limit duvarı geçer.
        var result = await svc.AskAsync(userId: 7, firmaId: 1, isAdmin: true, question: "Yeni soru");

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.NoHits);
    }
}
