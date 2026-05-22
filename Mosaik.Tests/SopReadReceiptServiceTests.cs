using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

/// <summary>
/// Plan 34 Faz D — SopReadReceiptService: Assign + MarkRead + Confirm + deadline pending query.
/// </summary>
public class SopReadReceiptServiceTests
{
    private sealed class TestContext : DbContext
    {
        public TestContext(DbContextOptions<TestContext> options) : base(options) { }

        public DbSet<SopDocument> SopDocuments => Set<SopDocument>();
        public DbSet<SopVersion> SopVersions => Set<SopVersion>();
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

    private static (TestContext db, SopReadReceiptService svc, FakeAudit audit) NewService(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var svc = new SopReadReceiptService(db, audit);
        return (db, svc, audit);
    }

    private static async Task<(int docId, int versionId)> SeedDocAndVersionAsync(TestContext db, int readDeadlineDays = 30)
    {
        var doc = new SopDocument
        {
            FirmaId = 1,
            Title = "Test SOP",
            OwnerUserId = 10,
            ReadDeadlineDays = readDeadlineDays,
            CreatedBy = 10
        };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();

        var version = new SopVersion
        {
            SopDocumentId = doc.Id,
            VersionNumber = 1,
            ContentJson = "<p>İçerik</p>",
            Status = 2,
            EffectiveDate = DateTime.UtcNow,
            CreatedBy = 10
        };
        db.SopVersions.Add(version);
        await db.SaveChangesAsync();
        return (doc.Id, version.Id);
    }

    [Fact]
    public async Task AssignToUsersAsync_NewUsers_CreatesReceipts()
    {
        var (db, svc, audit) = NewService(nameof(AssignToUsersAsync_NewUsers_CreatesReceipts));
        await using var _ = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        var added = await svc.AssignToUsersAsync(versionId, new[] { 1, 2, 3 });

        Assert.Equal(3, added);
        Assert.Equal(3, await db.SopReadReceipts.CountAsync());
        Assert.Contains("sop_assigned", audit.Events);
    }

    [Fact]
    public async Task AssignToUsersAsync_ExistingUsers_Idempotent()
    {
        var (db, svc, _) = NewService(nameof(AssignToUsersAsync_ExistingUsers_Idempotent));
        await using var _2 = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        await svc.AssignToUsersAsync(versionId, new[] { 1, 2 });
        var added = await svc.AssignToUsersAsync(versionId, new[] { 1, 2, 3 });

        Assert.Equal(1, added);                                       // sadece 3 yeni
        Assert.Equal(3, await db.SopReadReceipts.CountAsync());
    }

    [Fact]
    public async Task MarkReadAsync_SetsReadAt_OnlyOnce()
    {
        var (db, svc, audit) = NewService(nameof(MarkReadAsync_SetsReadAt_OnlyOnce));
        await using var _ = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        await svc.AssignToUsersAsync(versionId, new[] { 5 });

        await svc.MarkReadAsync(versionId, 5);
        var first = await svc.GetByVersionUserAsync(versionId, 5);
        var firstReadAt = first!.ReadAt;
        Assert.NotNull(firstReadAt);

        await Task.Delay(10);
        await svc.MarkReadAsync(versionId, 5);                         // ikinci kez — değişmemeli
        var second = await svc.GetByVersionUserAsync(versionId, 5);

        Assert.Equal(firstReadAt, second!.ReadAt);
        Assert.Contains("sop_read", audit.Events);
    }

    [Fact]
    public async Task MarkReadAsync_UnassignedUser_Noop()
    {
        var (db, svc, audit) = NewService(nameof(MarkReadAsync_UnassignedUser_Noop));
        await using var _ = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        await svc.MarkReadAsync(versionId, 99);                        // atanmamış user

        Assert.Empty(await db.SopReadReceipts.ToListAsync());
        Assert.DoesNotContain("sop_read", audit.Events);
    }

    [Fact]
    public async Task ConfirmAsync_SetsConfirmedAt_AndReadAtIfMissing()
    {
        var (db, svc, audit) = NewService(nameof(ConfirmAsync_SetsConfirmedAt_AndReadAtIfMissing));
        await using var _ = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        await svc.AssignToUsersAsync(versionId, new[] { 5 });

        var result = await svc.ConfirmAsync(versionId, 5);

        Assert.True(result.IsSuccess);
        var receipt = await svc.GetByVersionUserAsync(versionId, 5);
        Assert.NotNull(receipt!.ConfirmedAt);
        Assert.NotNull(receipt.ReadAt);                                // confirm okuma da sayar
        Assert.Contains("sop_confirmed", audit.Events);
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyConfirmed_NoOpSuccess()
    {
        var (db, svc, _) = NewService(nameof(ConfirmAsync_AlreadyConfirmed_NoOpSuccess));
        await using var _2 = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        await svc.AssignToUsersAsync(versionId, new[] { 5 });
        await svc.ConfirmAsync(versionId, 5);
        var secondResult = await svc.ConfirmAsync(versionId, 5);

        Assert.True(secondResult.IsSuccess);
    }

    [Fact]
    public async Task ConfirmAsync_Unassigned_Fails()
    {
        var (db, svc, _) = NewService(nameof(ConfirmAsync_Unassigned_Fails));
        await using var _2 = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db);
        var result = await svc.ConfirmAsync(versionId, 999);

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetMyReceiptsAsync_OnlyPending_FiltersConfirmed()
    {
        var (db, svc, _) = NewService(nameof(GetMyReceiptsAsync_OnlyPending_FiltersConfirmed));
        await using var _2 = db;

        var (_, v1) = await SeedDocAndVersionAsync(db);
        // ikinci doc/version
        var doc2 = new SopDocument { FirmaId = 1, Title = "Doc2", OwnerUserId = 10, ReadDeadlineDays = 30, CreatedBy = 10 };
        db.SopDocuments.Add(doc2); await db.SaveChangesAsync();
        var v2 = new SopVersion { SopDocumentId = doc2.Id, VersionNumber = 1, ContentJson = "<p>x</p>", Status = 2, CreatedBy = 10 };
        db.SopVersions.Add(v2); await db.SaveChangesAsync();

        await svc.AssignToUsersAsync(v1, new[] { 7 });
        await svc.AssignToUsersAsync(v2.Id, new[] { 7 });
        await svc.ConfirmAsync(v1, 7);

        var all = await svc.GetMyReceiptsAsync(7, onlyPending: false);
        var pending = await svc.GetMyReceiptsAsync(7, onlyPending: true);

        Assert.Equal(2, all.Count);
        Assert.Single(pending);
        Assert.Equal(v2.Id, pending[0].SopVersionId);
    }

    [Fact]
    public async Task GetPendingDueAsync_WithinWindow_Returns()
    {
        var (db, svc, _) = NewService(nameof(GetPendingDueAsync_WithinWindow_Returns));
        await using var _2 = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db, readDeadlineDays: 10);
        await svc.AssignToUsersAsync(versionId, new[] { 8 });

        // AssignedAt'i geriye al → deadline yaklaşsın
        var receipt = await db.SopReadReceipts.SingleAsync();
        receipt.AssignedAt = DateTime.UtcNow.AddDays(-5);              // 5 gün geçti, 5 gün kaldı
        await db.SaveChangesAsync();

        var due7 = await svc.GetPendingDueAsync(7);
        var due3 = await svc.GetPendingDueAsync(3);

        Assert.Single(due7);                                            // 5 gün ≤ 7 ✓
        Assert.Empty(due3);                                             // 5 gün > 3 ✗
    }

    [Fact]
    public async Task GetPendingDueAsync_AlreadyConfirmed_Excluded()
    {
        var (db, svc, _) = NewService(nameof(GetPendingDueAsync_AlreadyConfirmed_Excluded));
        await using var _2 = db;

        var (_, versionId) = await SeedDocAndVersionAsync(db, readDeadlineDays: 10);
        await svc.AssignToUsersAsync(versionId, new[] { 8 });
        var receipt = await db.SopReadReceipts.SingleAsync();
        receipt.AssignedAt = DateTime.UtcNow.AddDays(-5);
        await db.SaveChangesAsync();

        await svc.ConfirmAsync(versionId, 8);
        var due = await svc.GetPendingDueAsync(7);

        Assert.Empty(due);
    }
}
