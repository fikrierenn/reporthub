using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Logging;
using Mosaik.Core.Notification;
using Mosaik.Core.Users;
using Mosaik.Core.Workflow;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34 Faz E S-20 — SopApprovalService.DecideAsync dispatch akışı.
// Final Approved step → DispatchPublishedAsync (assign + notification).
// Rejected step → dispatch yok. IsCompanyWide=false → audit skip.
public class SopApprovalServiceTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbSet<SopDocument> SopDocuments => Set<SopDocument>();
        public DbSet<SopVersion> SopVersions => Set<SopVersion>();
        public DbSet<SopReadReceipt> SopReadReceipts => Set<SopReadReceipt>();
        public DbSet<SopApprovalSubmission> SopApprovalSubmissions => Set<SopApprovalSubmission>();
        public DbSet<SopChunk> SopChunks => Set<SopChunk>();
        public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
        public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            new Mosaik.Modules.SOP.SopModule().ConfigureModelBuilder(mb);
            mb.Entity<ApprovalRequest>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
            });
            mb.Entity<ApprovalStep>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(s => s.Request)
                    .WithMany(r => r.Steps)
                    .HasForeignKey(s => s.RequestId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
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

    private sealed class FakeNotifications : INotificationService
    {
        public List<(List<int> Ids, string EntityType, int? EntityId, string? NotificationType)> BulkCalls { get; } = new();

        public Task<int> CreateBulkAsync(IEnumerable<int> userIds, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
        {
            var ids = userIds.ToList();
            BulkCalls.Add((ids, entityType, entityId, notificationType));
            return Task.FromResult(ids.Count);
        }

        public Task<Notification> CreateAsync(int userId, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
            => throw new NotImplementedException();

        public Task<int> NotifyAllActiveUsersAsync(string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
            => throw new NotImplementedException();

        public Task<int> GetUnreadCountAsync(int userId) => throw new NotImplementedException();
        public Task<List<Notification>> GetRecentAsync(int userId, int take = 30, bool unreadOnly = false) => throw new NotImplementedException();
        public Task<bool> MarkAsReadAsync(int notificationId, int userId) => throw new NotImplementedException();
        public Task<int> MarkAllAsReadAsync(int userId) => throw new NotImplementedException();
        public Task<int> CreateBulkIfNotExistsAsync(string externalKeyPrefix, IEnumerable<int> userIds,
            string entityType, int? entityId, string title, string? message,
            string? targetUrl, string? notificationType, string? createdBy)
            => throw new NotImplementedException();
    }

    private sealed class FakeUserDirectory : IActiveUserDirectory
    {
        public List<int> UserIds { get; set; } = new();
        public int? LastFirmaQueried { get; private set; }

        public Task<List<int>> GetActiveUserIdsAsync(int? firmaId = null)
        {
            LastFirmaQueried = firmaId;
            return Task.FromResult(UserIds.ToList());
        }

        public Task<Dictionary<int, string>> GetUserEmailsAsync(IEnumerable<int> userIds)
            => Task.FromResult(new Dictionary<int, string>());

        public Task<List<Mosaik.Core.Users.ActiveUserInfo>> GetActiveUsersAsync(int? firmaId = null)
            => Task.FromResult(new List<Mosaik.Core.Users.ActiveUserInfo>());
    }

    private sealed class FakeEmbedder : Mosaik.Core.AI.Embed.IMosaikEmbedder
    {
        public int Dimensions => 3;
        public bool IsReady => false;                                       // hazır değil — indexer skip
        public Task<float[]> EmbedAsync(string text, Mosaik.Core.AI.Embed.EmbedRole role, CancellationToken ct = default)
            => throw new NotImplementedException();
        public Task<float[][]> EmbedBatchAsync(IReadOnlyList<string> texts, Mosaik.Core.AI.Embed.EmbedRole role, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private static (TestContext db, SopApprovalService approval, SopService sop, SopReadReceiptService receipts,
                    FakeAudit audit, FakeNotifications notif, FakeUserDirectory dir) NewService(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var notif = new FakeNotifications();
        var dir = new FakeUserDirectory();
        var sop = new SopService(db, audit);
        var receipts = new SopReadReceiptService(db, audit);
        var indexer = new Mosaik.Modules.SOP.Services.SopIndexer(db, new FakeEmbedder(), audit,
            NullLogger<Mosaik.Modules.SOP.Services.SopIndexer>.Instance);
        var approval = new SopApprovalService(db, sop, audit, receipts, notif, dir, indexer,
            NullLogger<SopApprovalService>.Instance);
        return (db, approval, sop, receipts, audit, notif, dir);
    }

    private static async Task<(SopDocument doc, SopVersion version)> SeedPendingVersionAsync(
        TestContext db, bool isCompanyWide, bool requiresIK = false)
    {
        var doc = new SopDocument
        {
            FirmaId = 7,
            Title = "Test SOP",
            OwnerUserId = 10,
            ReadDeadlineDays = 30,
            IsCompanyWide = isCompanyWide,
            DepartmentIds = isCompanyWide ? null : "1,2",
            RequiresIKApproval = requiresIK,
            CreatedBy = 10
        };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();

        var version = new SopVersion
        {
            SopDocumentId = doc.Id,
            VersionNumber = 1,
            ContentJson = "<p>içerik</p>",
            PlainTextContent = "içerik",
            Status = 1,                                                  // Pending
            CreatedBy = 10
        };
        db.SopVersions.Add(version);
        await db.SaveChangesAsync();
        return (doc, version);
    }

    private static async Task<int> SeedSingleStepRequestAsync(TestContext db, int versionId)
    {
        var now = DateTime.UtcNow;
        var req = new ApprovalRequest
        {
            EntityType = "sop_version",
            EntityId = versionId,
            Status = ApprovalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "10"
        };
        req.Steps.Add(new ApprovalStep
        {
            StepOrder = 10,
            ApproverUserId = 10,
            Status = ApprovalStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = "10"
        });
        db.ApprovalRequests.Add(req);
        await db.SaveChangesAsync();
        return req.Steps.First().Id;
    }

    [Fact]
    public async Task DecideAsync_FinalApprove_CompanyWide_DispatchesAssignAndNotify()
    {
        var (db, approval, _, _, audit, notif, dir) =
            NewService(nameof(DecideAsync_FinalApprove_CompanyWide_DispatchesAssignAndNotify));
        await using var _ = db;

        var (doc, version) = await SeedPendingVersionAsync(db, isCompanyWide: true);
        var stepId = await SeedSingleStepRequestAsync(db, version.Id);
        dir.UserIds = new List<int> { 11, 12, 13 };

        var result = await approval.DecideAsync(stepId, ApprovalStatus.Approved, decidingUserId: 99);

        Assert.True(result.IsSuccess);
        Assert.Equal(doc.FirmaId, dir.LastFirmaQueried);

        var receipts = await db.SopReadReceipts.AsNoTracking().ToListAsync();
        Assert.Equal(3, receipts.Count);
        Assert.Equal(new[] { 11, 12, 13 }, receipts.Select(r => r.UserId).OrderBy(x => x));

        Assert.Single(notif.BulkCalls);
        Assert.Equal("sop_version", notif.BulkCalls[0].EntityType);
        Assert.Equal(version.Id, notif.BulkCalls[0].EntityId);
        Assert.Equal("SopPublished", notif.BulkCalls[0].NotificationType);
        Assert.Equal(3, notif.BulkCalls[0].Ids.Count);

        Assert.Contains("sop_published", audit.Events);
        Assert.DoesNotContain("sop_dispatch_skipped", audit.Events);
    }

    [Fact]
    public async Task DecideAsync_FinalApprove_DepartmentScoped_LogsSkipAndSkipsDispatch()
    {
        var (db, approval, _, _, audit, notif, dir) =
            NewService(nameof(DecideAsync_FinalApprove_DepartmentScoped_LogsSkipAndSkipsDispatch));
        await using var _ = db;

        var (_, version) = await SeedPendingVersionAsync(db, isCompanyWide: false);
        var stepId = await SeedSingleStepRequestAsync(db, version.Id);
        dir.UserIds = new List<int> { 11, 12 };                          // her ihtimale set; çağrılmamalı

        var result = await approval.DecideAsync(stepId, ApprovalStatus.Approved, decidingUserId: 99);

        Assert.True(result.IsSuccess);
        Assert.Null(dir.LastFirmaQueried);                               // GetActiveUserIdsAsync çağrılmadı
        Assert.Empty(notif.BulkCalls);
        Assert.Empty(await db.SopReadReceipts.ToListAsync());
        Assert.Contains("sop_dispatch_skipped", audit.Events);
        Assert.DoesNotContain("sop_published", audit.Events);
    }

    [Fact]
    public async Task DecideAsync_Reject_DoesNotDispatch()
    {
        var (db, approval, _, _, audit, notif, dir) =
            NewService(nameof(DecideAsync_Reject_DoesNotDispatch));
        await using var _ = db;

        var (_, version) = await SeedPendingVersionAsync(db, isCompanyWide: true);
        var stepId = await SeedSingleStepRequestAsync(db, version.Id);
        dir.UserIds = new List<int> { 11 };

        var result = await approval.DecideAsync(stepId, ApprovalStatus.Rejected, decidingUserId: 99, comment: "düzelt");

        Assert.True(result.IsSuccess);
        Assert.Null(dir.LastFirmaQueried);
        Assert.Empty(notif.BulkCalls);
        Assert.Empty(await db.SopReadReceipts.ToListAsync());
        Assert.Contains("sop_version_rejected", audit.Events);
        Assert.DoesNotContain("sop_published", audit.Events);
    }

    // ---------- SubmitAsync ----------

    [Fact]
    public async Task SubmitAsync_DraftVersion_CreatesRequestAndPendingSteps()
    {
        var (db, approval, _, _, audit, _, _) = NewService(nameof(SubmitAsync_DraftVersion_CreatesRequestAndPendingSteps));
        await using var _2 = db;

        // Draft versiyon yarat (SeedPendingVersion Pending üretiyor — manuel yapayım)
        var doc = new SopDocument { FirmaId = 7, Title = "X", OwnerUserId = 10, IsActive = true, IsCompanyWide = true, RequiresIKApproval = true, CreatedBy = 10 };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();
        var v = new SopVersion { SopDocumentId = doc.Id, VersionNumber = 1, ContentJson = "<p>x</p>", Status = 0, CreatedBy = 10 };
        db.SopVersions.Add(v);
        await db.SaveChangesAsync();

        var result = await approval.SubmitAsync(v.Id, submittedBy: 10);

        Assert.True(result.IsSuccess);
        var refreshedVersion = await db.SopVersions.FindAsync(v.Id);
        Assert.Equal((byte)1, refreshedVersion!.Status);                        // Pending
        var req = await db.ApprovalRequests.Include(r => r.Steps).SingleAsync();
        Assert.Equal("sop_version", req.EntityType);
        Assert.Equal(v.Id, req.EntityId);
        Assert.Equal(3, req.Steps.Count);                                       // Yazan + DepYön + IK
        Assert.Single(await db.SopApprovalSubmissions.ToListAsync());
    }

    [Fact]
    public async Task SubmitAsync_DraftVersion_NoIKApproval_TwoSteps()
    {
        var (db, approval, _, _, _, _, _) = NewService(nameof(SubmitAsync_DraftVersion_NoIKApproval_TwoSteps));
        await using var _ = db;

        var doc = new SopDocument { FirmaId = 7, Title = "X", OwnerUserId = 10, IsActive = true, IsCompanyWide = true, RequiresIKApproval = false, CreatedBy = 10 };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();
        var v = new SopVersion { SopDocumentId = doc.Id, VersionNumber = 1, ContentJson = "<p>x</p>", Status = 0, CreatedBy = 10 };
        db.SopVersions.Add(v);
        await db.SaveChangesAsync();

        var result = await approval.SubmitAsync(v.Id, submittedBy: 10);

        Assert.True(result.IsSuccess);
        var req = await db.ApprovalRequests.Include(r => r.Steps).SingleAsync();
        Assert.Equal(2, req.Steps.Count);                                       // Yazan + DepYön, IK yok
    }

    [Fact]
    public async Task SubmitAsync_PendingVersion_Rejected()
    {
        var (db, approval, _, _, _, _, _) = NewService(nameof(SubmitAsync_PendingVersion_Rejected));
        await using var _ = db;
        var (_, version) = await SeedPendingVersionAsync(db, isCompanyWide: true);

        var result = await approval.SubmitAsync(version.Id, submittedBy: 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("Draft", result.Message);
    }

    [Fact]
    public async Task SubmitAsync_NonexistentVersion_Fails()
    {
        var (db, approval, _, _, _, _, _) = NewService(nameof(SubmitAsync_NonexistentVersion_Fails));
        await using var _ = db;

        var result = await approval.SubmitAsync(versionId: 9999, submittedBy: 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("bulunamadı", result.Message);
    }
}
