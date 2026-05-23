using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.AI.Local;
using Mosaik.Core.AI.Skills;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Save-time AI Enrichment — ReviewAsync + AcceptAsync davranışları.
// Pattern: SopRagAdvisorServiceTests'teki TestContext + FakeAudit + InMemory.
public class SopEnrichmentServiceTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbSet<SopDocument> SopDocuments => Set<SopDocument>();
        public DbSet<SopVersion> SopVersions => Set<SopVersion>();
        public DbSet<SopEnrichmentReport> SopEnrichmentReports => Set<SopEnrichmentReport>();

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
        public string Answer { get; set; } = """
            {
              "suggested_tags": ["hijyen","gıda"],
              "suggested_category": "Operasyon",
              "missing_sections": [],
              "anglo_jargon": [],
              "kvkk_issues": [],
              "retention_suggestions": [],
              "legal_basis_gaps": [],
              "severity": "yellow",
              "overall_score": 7,
              "summary": "Genel olarak iyi."
            }
            """;
        public bool ShouldFail { get; set; }
        public int CallCount { get; private set; }

        public Task<LlmRunResult> RunAsync(string systemPrompt, string userPrompt, LlmRunOptions options, CancellationToken ct = default)
        {
            CallCount++;
            if (ShouldFail) return Task.FromResult(new LlmRunResult(false, null, 0, 0, "Mock fail"));
            return Task.FromResult(new LlmRunResult(true, Answer, 100, 50));
        }
    }

    private static (TestContext db, SopEnrichmentService svc, FakeAudit audit, FakeLlm llm) NewService(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var llm = new FakeLlm();
        var skills = new EmptySkillCatalog();
        var svc = new SopEnrichmentService(db, llm, skills, audit, NullLogger<SopEnrichmentService>.Instance);
        return (db, svc, audit, llm);
    }

    private static async Task<SopVersion> SeedVersionAsync(TestContext db, string title = "Test SOP", string? plainText = "Bu bir test prosedürüdür. Hijyen kurallarını anlatır.", string? category = "Operasyon")
    {
        var doc = new SopDocument
        {
            FirmaId = 1,
            Title = title,
            Category = category,
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
            ContentJson = "<p>test</p>",
            PlainTextContent = plainText,
            Status = 0,
            CreatedBy = 10
        };
        db.SopVersions.Add(version);
        await db.SaveChangesAsync();
        return version;
    }

    [Fact]
    public async Task Review_LlmNotReady_Fails()
    {
        var (db, svc, audit, llm) = NewService(nameof(Review_LlmNotReady_Fails));
        await using var _ = db;
        var v = await SeedVersionAsync(db);
        llm.IsReady = false;

        var reportId = await svc.ReviewAsync(v.Id);

        var report = await db.SopEnrichmentReports.FindAsync(reportId);
        Assert.NotNull(report);
        Assert.Equal((byte)2, report!.Status);                                  // Failed
        Assert.Contains("LLM", report.FailureReason ?? "");
        Assert.Contains("sop_enrichment_failed", audit.Events);
    }

    [Fact]
    public async Task Review_EmptyPlainText_Fails()
    {
        var (db, svc, _, _) = NewService(nameof(Review_EmptyPlainText_Fails));
        await using var _ = db;
        var v = await SeedVersionAsync(db, plainText: "   ");

        var reportId = await svc.ReviewAsync(v.Id);

        var report = await db.SopEnrichmentReports.FindAsync(reportId);
        Assert.Equal((byte)2, report!.Status);
        Assert.Contains("boş", report.FailureReason ?? "");
    }

    [Fact]
    public async Task Review_VersionNotFound_Fails()
    {
        var (db, svc, _, _) = NewService(nameof(Review_VersionNotFound_Fails));
        await using var _ = db;

        var reportId = await svc.ReviewAsync(versionId: 9999);

        var report = await db.SopEnrichmentReports.FindAsync(reportId);
        Assert.Equal((byte)2, report!.Status);
    }

    [Fact]
    public async Task Review_Success_PersistsJsonAndAudit()
    {
        var (db, svc, audit, llm) = NewService(nameof(Review_Success_PersistsJsonAndAudit));
        await using var _ = db;
        var v = await SeedVersionAsync(db);

        var reportId = await svc.ReviewAsync(v.Id);

        var report = await db.SopEnrichmentReports.FindAsync(reportId);
        Assert.Equal((byte)1, report!.Status);                                  // Completed
        Assert.NotNull(report.CompletedAt);

        using var jdoc = JsonDocument.Parse(report.ReportJson);
        Assert.Equal("Operasyon", jdoc.RootElement.GetProperty("suggested_category").GetString());
        Assert.Equal(7, jdoc.RootElement.GetProperty("overall_score").GetInt32());

        Assert.Contains("sop_enrichment_completed", audit.Events);
        Assert.Equal(1, llm.CallCount);
    }

    [Fact]
    public async Task Review_LlmReturnsInvalidJson_Fails()
    {
        var (db, svc, _, llm) = NewService(nameof(Review_LlmReturnsInvalidJson_Fails));
        await using var _ = db;
        var v = await SeedVersionAsync(db);
        llm.Answer = "bu JSON değil, sadece düz metin";

        var reportId = await svc.ReviewAsync(v.Id);

        var report = await db.SopEnrichmentReports.FindAsync(reportId);
        Assert.Equal((byte)2, report!.Status);
        Assert.Contains("JSON", report.FailureReason ?? "");
    }

    [Fact]
    public async Task Review_LlmReturnsJsonWithMarkdownFence_Succeeds()
    {
        var (db, svc, _, llm) = NewService(nameof(Review_LlmReturnsJsonWithMarkdownFence_Succeeds));
        await using var _ = db;
        var v = await SeedVersionAsync(db);
        llm.Answer = "```json\n{\"severity\":\"green\",\"overall_score\":9,\"summary\":\"İyi.\"}\n```";

        var reportId = await svc.ReviewAsync(v.Id);

        var report = await db.SopEnrichmentReports.FindAsync(reportId);
        Assert.Equal((byte)1, report!.Status);
        using var jdoc = JsonDocument.Parse(report.ReportJson);
        Assert.Equal("green", jdoc.RootElement.GetProperty("severity").GetString());
    }

    [Fact]
    public async Task Accept_NonexistentReport_Fails()
    {
        var (db, svc, _, _) = NewService(nameof(Accept_NonexistentReport_Fails));
        await using var _ = db;

        var (ok, message) = await svc.AcceptAsync(reportId: 9999, userId: 1);

        Assert.False(ok);
        Assert.Contains("bulunamadı", message);
    }

    [Fact]
    public async Task Accept_PendingReport_Rejected()
    {
        var (db, svc, _, _) = NewService(nameof(Accept_PendingReport_Rejected));
        await using var _ = db;
        var v = await SeedVersionAsync(db);
        var report = new SopEnrichmentReport
        {
            SopVersionId = v.Id,
            ReportJson = "{}",
            Status = 0,                                                         // Pending
            CreatedAt = DateTime.UtcNow
        };
        db.SopEnrichmentReports.Add(report);
        await db.SaveChangesAsync();

        var (ok, _) = await svc.AcceptAsync(report.Id, userId: 1);

        Assert.False(ok);
        var refreshed = await db.SopEnrichmentReports.FindAsync(report.Id);
        Assert.Equal((byte)0, refreshed!.Status);                               // değişmedi
    }

    [Fact]
    public async Task Accept_WhitelistedCategory_AppliedToDocument()
    {
        var (db, svc, audit, _) = NewService(nameof(Accept_WhitelistedCategory_AppliedToDocument));
        await using var _ = db;
        var v = await SeedVersionAsync(db, category: "İK");                     // başlangıç
        var report = new SopEnrichmentReport
        {
            SopVersionId = v.Id,
            ReportJson = """{"suggested_category":"Finans","severity":"yellow"}""",
            Status = 1,                                                         // Completed
            CreatedAt = DateTime.UtcNow
        };
        db.SopEnrichmentReports.Add(report);
        await db.SaveChangesAsync();

        var (ok, message) = await svc.AcceptAsync(report.Id, userId: 42);

        Assert.True(ok);
        Assert.Contains("Finans", message);

        var refreshedReport = await db.SopEnrichmentReports.FindAsync(report.Id);
        Assert.Equal((byte)3, refreshedReport!.Status);                         // Accepted
        Assert.Equal(42, refreshedReport.AcceptedByUserId);
        Assert.NotNull(refreshedReport.AcceptedAt);

        var refreshedDoc = await db.SopDocuments.FindAsync(v.SopDocumentId);
        Assert.Equal("Finans", refreshedDoc!.Category);

        Assert.Contains("sop_enrichment_accepted", audit.Events);
    }

    [Fact]
    public async Task Accept_InvalidCategory_AcceptedButCategoryUnchanged()
    {
        var (db, svc, _, _) = NewService(nameof(Accept_InvalidCategory_AcceptedButCategoryUnchanged));
        await using var _ = db;
        var v = await SeedVersionAsync(db, category: "İK");
        var report = new SopEnrichmentReport
        {
            SopVersionId = v.Id,
            ReportJson = """{"suggested_category":"YANLIŞ_KATEGORİ"}""",
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };
        db.SopEnrichmentReports.Add(report);
        await db.SaveChangesAsync();

        var (ok, message) = await svc.AcceptAsync(report.Id, userId: 1);

        Assert.True(ok);
        Assert.Contains("kategori değişmedi", message);

        var refreshedDoc = await db.SopDocuments.FindAsync(v.SopDocumentId);
        Assert.Equal("İK", refreshedDoc!.Category);                             // değişmedi
    }

    [Fact]
    public async Task Accept_MalformedJson_StillAcceptsReport()
    {
        var (db, svc, _, _) = NewService(nameof(Accept_MalformedJson_StillAcceptsReport));
        await using var _ = db;
        var v = await SeedVersionAsync(db);
        var report = new SopEnrichmentReport
        {
            SopVersionId = v.Id,
            ReportJson = "{ malformed",
            Status = 1,
            CreatedAt = DateTime.UtcNow
        };
        db.SopEnrichmentReports.Add(report);
        await db.SaveChangesAsync();

        var (ok, _) = await svc.AcceptAsync(report.Id, userId: 1);

        Assert.True(ok);                                                        // malformed JSON kabul akışını bozmaz
        var refreshed = await db.SopEnrichmentReports.FindAsync(report.Id);
        Assert.Equal((byte)3, refreshed!.Status);
    }
}
