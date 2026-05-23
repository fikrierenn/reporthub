using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Faz 6 A-28 — KVKK 1-yıl retention cleanup.
public class SopAiHistoryCleanupJobTests
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

    private static (TestContext db, SopAiHistoryCleanupJob job, FakeAudit audit) NewJob(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var job = new SopAiHistoryCleanupJob(db, audit, NullLogger<SopAiHistoryCleanupJob>.Instance);
        return (db, job, audit);
    }

    [Fact]
    public async Task Execute_NoOldRecords_DeletesZero()
    {
        var (db, job, audit) = NewJob(nameof(Execute_NoOldRecords_DeletesZero));
        await using var _ = db;

        db.SopAiConversations.Add(new SopAiConversation
        {
            UserId = 1, Question = "q", Answer = "a",
            CreatedAt = DateTime.UtcNow.AddDays(-30)                       // güncel
        });
        await db.SaveChangesAsync();

        var deleted = await job.ExecuteAsync();

        Assert.Equal(0, deleted);
        Assert.Single(await db.SopAiConversations.ToListAsync());
        Assert.DoesNotContain("sop_ai_history_purged", audit.Events);
    }

    [Fact]
    public async Task Execute_OldRecordsBeyondYear_Deleted()
    {
        var (db, job, audit) = NewJob(nameof(Execute_OldRecordsBeyondYear_Deleted));
        await using var _ = db;

        db.SopAiConversations.AddRange(
            new SopAiConversation { UserId = 1, Question = "old1", Answer = "a", CreatedAt = DateTime.UtcNow.AddDays(-400) },
            new SopAiConversation { UserId = 1, Question = "old2", Answer = "a", CreatedAt = DateTime.UtcNow.AddDays(-500) },
            new SopAiConversation { UserId = 1, Question = "new",  Answer = "a", CreatedAt = DateTime.UtcNow.AddDays(-30) });
        await db.SaveChangesAsync();

        var deleted = await job.ExecuteAsync();

        Assert.Equal(2, deleted);
        var remaining = await db.SopAiConversations.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal("new", remaining[0].Question);
        Assert.Contains("sop_ai_history_purged", audit.Events);
    }

    [Fact]
    public async Task Execute_EmptyTable_DeletesZero()
    {
        var (db, job, audit) = NewJob(nameof(Execute_EmptyTable_DeletesZero));
        await using var _ = db;

        var deleted = await job.ExecuteAsync();

        Assert.Equal(0, deleted);
        Assert.DoesNotContain("sop_ai_history_purged", audit.Events);
    }
}
