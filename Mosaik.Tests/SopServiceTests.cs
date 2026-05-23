using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Logging;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

/// <summary>
/// Plan 34 Faz C — SopService version chain + supersede + archive sweep.
/// In-memory DbContext (DbContextOptions ile generic DbContext yetmiyor — MosaikContext
/// modül entity'lerini ConfigureModelBuilder ile yüklediğinden TestContext kullanıyoruz).
/// </summary>
public class SopServiceTests
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
            var module = new Mosaik.Modules.SOP.SopModule();
            module.ConfigureModelBuilder(mb);
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

    private static (TestContext db, SopService svc, FakeAudit audit) NewService(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var svc = new SopService(db, audit);
        return (db, svc, audit);
    }

    private static SopDocumentInput SampleInput() =>
        new(FirmaId: 1, Title: "Test SOP", Description: "açıklama",
            Category: "ik", DepartmentIds: "1,2", IsCompanyWide: false,
            OwnerUserId: 10, ReadDeadlineDays: 30, RequiresIKApproval: true, AiAdvisorEnabled: true);

    [Fact]
    public async Task Create_PersistsDocumentAndLogsAudit()
    {
        var (db, svc, audit) = NewService(nameof(Create_PersistsDocumentAndLogsAudit));
        await using var _ = db;

        var result = await svc.CreateAsync(SampleInput(), createdBy: 10);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        var saved = await db.SopDocuments.SingleAsync();
        Assert.Equal("Test SOP", saved.Title);
        Assert.Equal("1,2", saved.DepartmentIds);
        Assert.False(saved.IsCompanyWide);
        Assert.Contains("sop_created", audit.Events);
    }

    [Fact]
    public async Task Create_CompanyWide_ClearsDepartmentIds()
    {
        var (db, svc, _) = NewService(nameof(Create_CompanyWide_ClearsDepartmentIds));
        await using var _2 = db;

        var input = SampleInput() with { IsCompanyWide = true, DepartmentIds = "1,2" };
        var result = await svc.CreateAsync(input, createdBy: 10);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Data!.DepartmentIds);
        Assert.True(result.Data.IsCompanyWide);
    }

    [Fact]
    public async Task Create_NoDepartmentAndNotCompanyWide_Fails()
    {
        var (db, svc, _) = NewService(nameof(Create_NoDepartmentAndNotCompanyWide_Fails));
        await using var _2 = db;

        var input = SampleInput() with { IsCompanyWide = false, DepartmentIds = null };
        var result = await svc.CreateAsync(input, createdBy: 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("departman", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task NewVersion_AssignsIncrementalNumber()
    {
        var (db, svc, _) = NewService(nameof(NewVersion_AssignsIncrementalNumber));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var v1 = (await svc.NewVersionAsync(doc.Id, "<p>v1</p>", createdBy: 10)).Data!;
        var v2 = (await svc.NewVersionAsync(doc.Id, "<p>v2</p>", createdBy: 10)).Data!;
        var v3 = (await svc.NewVersionAsync(doc.Id, "<p>v3</p>", createdBy: 10)).Data!;

        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal(2, v2.VersionNumber);
        Assert.Equal(3, v3.VersionNumber);
        Assert.Equal(0, v1.Status);  // Draft
        Assert.NotNull(v1.PlainTextContent);
    }

    [Fact]
    public async Task NewVersion_SanitizesScriptTags()
    {
        var (db, svc, _) = NewService(nameof(NewVersion_SanitizesScriptTags));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var malicious = "<p>safe</p><script>alert('xss')</script>";
        var v = (await svc.NewVersionAsync(doc.Id, malicious, createdBy: 10)).Data!;

        Assert.DoesNotContain("<script", v.ContentJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<p>safe</p>", v.ContentJson);
    }

    [Fact]
    public async Task MarkVersionApproved_SetsActive_AndSupersedesOldVersion()
    {
        var (db, svc, audit) = NewService(nameof(MarkVersionApproved_SetsActive_AndSupersedesOldVersion));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var v1 = (await svc.NewVersionAsync(doc.Id, "<p>v1</p>", createdBy: 10)).Data!;
        v1.Status = 1; // Pending (manuel — SopApprovalService akışı yerine)
        await db.SaveChangesAsync();
        await svc.MarkVersionApprovedAsync(v1.Id);

        var v2 = (await svc.NewVersionAsync(doc.Id, "<p>v2</p>", createdBy: 10)).Data!;
        v2.Status = 1;
        await db.SaveChangesAsync();
        await svc.MarkVersionApprovedAsync(v2.Id);

        var v1Reloaded = await db.SopVersions.FindAsync(v1.Id);
        var v2Reloaded = await db.SopVersions.FindAsync(v2.Id);

        Assert.Equal((byte)2, v1Reloaded!.Status);
        Assert.NotNull(v1Reloaded.SupersededDate);
        Assert.Equal((byte)2, v2Reloaded!.Status);
        Assert.Null(v2Reloaded.SupersededDate);

        var active = await svc.GetActiveVersionAsync(doc.Id);
        Assert.NotNull(active);
        Assert.Equal(v2.Id, active!.Id);
        Assert.Contains("sop_version_approved", audit.Events);
    }

    [Fact]
    public async Task MarkVersionApproved_DraftStatus_Fails()
    {
        var (db, svc, _) = NewService(nameof(MarkVersionApproved_DraftStatus_Fails));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var v = (await svc.NewVersionAsync(doc.Id, "<p>x</p>", createdBy: 10)).Data!;

        var result = await svc.MarkVersionApprovedAsync(v.Id);

        Assert.False(result.IsSuccess);
        Assert.Contains("Pending", result.Message);
    }

    [Fact]
    public async Task ArchiveSupersededAsync_OldSupersededVersions_BecomeArchived()
    {
        var (db, svc, audit) = NewService(nameof(ArchiveSupersededAsync_OldSupersededVersions_BecomeArchived));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var v = (await svc.NewVersionAsync(doc.Id, "<p>x</p>", createdBy: 10)).Data!;
        v.Status = 2;
        v.SupersededDate = DateTime.UtcNow.AddDays(-100);  // 100 gün önce supersede
        await db.SaveChangesAsync();

        var count = await svc.ArchiveSupersededAsync();

        Assert.Equal(1, count);
        var reloaded = await db.SopVersions.FindAsync(v.Id);
        Assert.Equal((byte)3, reloaded!.Status);
        Assert.Contains("sop_archive_sweep", audit.Events);
    }

    [Fact]
    public async Task ArchiveSupersededAsync_RecentSupersededVersions_NotArchived()
    {
        var (db, svc, _) = NewService(nameof(ArchiveSupersededAsync_RecentSupersededVersions_NotArchived));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var v = (await svc.NewVersionAsync(doc.Id, "<p>x</p>", createdBy: 10)).Data!;
        v.Status = 2;
        v.SupersededDate = DateTime.UtcNow.AddDays(-30);  // 30 gün — eşik altı (90)
        await db.SaveChangesAsync();

        var count = await svc.ArchiveSupersededAsync();

        Assert.Equal(0, count);
        var reloaded = await db.SopVersions.FindAsync(v.Id);
        Assert.Equal((byte)2, reloaded!.Status);
    }

    [Fact]
    public async Task Update_InvalidTitle_Fails()
    {
        var (db, svc, _) = NewService(nameof(Update_InvalidTitle_Fails));
        await using var _2 = db;

        var doc = (await svc.CreateAsync(SampleInput(), createdBy: 10)).Data!;
        var bad = SampleInput() with { Title = "" };
        var result = await svc.UpdateAsync(doc.Id, bad, updatedBy: 10);

        Assert.False(result.IsSuccess);
    }

    // ---------- Plan 34.1: Soft/Hard Delete + Restore ----------

    [Fact]
    public async Task SoftDelete_ActiveDocument_SetsInactiveAndLogsAudit()
    {
        var (db, svc, audit) = NewService(nameof(SoftDelete_ActiveDocument_SetsInactiveAndLogsAudit));
        await using var _ = db;
        var createResult = await svc.CreateAsync(SampleInput(), createdBy: 10);
        var doc = createResult.Data!;

        var result = await svc.SoftDeleteAsync(doc.Id, deletedBy: 10);

        Assert.True(result.IsSuccess);
        var refreshed = await db.SopDocuments.FindAsync(doc.Id);
        Assert.False(refreshed!.IsActive);
        Assert.Contains("sop_archived", audit.Events);
    }

    [Fact]
    public async Task SoftDelete_AlreadyArchived_IdempotentOk()
    {
        var (db, svc, _) = NewService(nameof(SoftDelete_AlreadyArchived_IdempotentOk));
        await using var _2 = db;
        var doc = (await svc.CreateAsync(SampleInput(), 10)).Data!;
        await svc.SoftDeleteAsync(doc.Id, 10);

        var second = await svc.SoftDeleteAsync(doc.Id, 10);

        Assert.True(second.IsSuccess);
        Assert.Contains("arşivlenmiş", second.Message);
    }

    [Fact]
    public async Task SoftDelete_NonexistentDocument_Fails()
    {
        var (db, svc, _) = NewService(nameof(SoftDelete_NonexistentDocument_Fails));
        await using var _ = db;

        var result = await svc.SoftDeleteAsync(id: 9999, deletedBy: 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("bulunamadı", result.Message);
    }

    [Fact]
    public async Task Restore_ArchivedDocument_SetsActiveAndLogsAudit()
    {
        var (db, svc, audit) = NewService(nameof(Restore_ArchivedDocument_SetsActiveAndLogsAudit));
        await using var _ = db;
        var doc = (await svc.CreateAsync(SampleInput(), 10)).Data!;
        await svc.SoftDeleteAsync(doc.Id, 10);

        var result = await svc.RestoreAsync(doc.Id, restoredBy: 10);

        Assert.True(result.IsSuccess);
        var refreshed = await db.SopDocuments.FindAsync(doc.Id);
        Assert.True(refreshed!.IsActive);
        Assert.Contains("sop_restored", audit.Events);
    }

    [Fact]
    public async Task Restore_AlreadyActive_IdempotentOk()
    {
        var (db, svc, _) = NewService(nameof(Restore_AlreadyActive_IdempotentOk));
        await using var _ = db;
        var doc = (await svc.CreateAsync(SampleInput(), 10)).Data!;

        var result = await svc.RestoreAsync(doc.Id, 10);

        Assert.True(result.IsSuccess);
        Assert.Contains("aktif", result.Message);
    }

    [Fact]
    public async Task HardDelete_DraftOnlyDocument_RemovesEntity()
    {
        var (db, svc, audit) = NewService(nameof(HardDelete_DraftOnlyDocument_RemovesEntity));
        await using var _ = db;
        var doc = (await svc.CreateAsync(SampleInput(), 10)).Data!;
        await svc.NewVersionAsync(doc.Id, "<p>içerik</p>", createdBy: 10);

        var result = await svc.HardDeleteAsync(doc.Id, deletedBy: 10);

        Assert.True(result.IsSuccess);
        Assert.Null(await db.SopDocuments.FindAsync(doc.Id));
        Assert.Contains("sop_hard_deleted", audit.Events);
    }

    [Fact]
    public async Task HardDelete_DocumentWithApprovedVersion_Blocked()
    {
        var (db, svc, _) = NewService(nameof(HardDelete_DocumentWithApprovedVersion_Blocked));
        await using var _ = db;
        var doc = (await svc.CreateAsync(SampleInput(), 10)).Data!;
        var v = (await svc.NewVersionAsync(doc.Id, "<p>içerik</p>", 10)).Data!;
        // Draft (0) → Pending (1) → Approved (2) — service guard zinciri.
        v.Status = 1;
        await db.SaveChangesAsync();
        await svc.MarkVersionApprovedAsync(v.Id);

        var result = await svc.HardDeleteAsync(doc.Id, 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("kalıcı silinemez", result.Message);
        Assert.NotNull(await db.SopDocuments.FindAsync(doc.Id));                // hala duruyor
    }

    [Fact]
    public async Task HardDelete_NonexistentDocument_Fails()
    {
        var (db, svc, _) = NewService(nameof(HardDelete_NonexistentDocument_Fails));
        await using var _ = db;

        var result = await svc.HardDeleteAsync(id: 9999, deletedBy: 10);

        Assert.False(result.IsSuccess);
    }
}
