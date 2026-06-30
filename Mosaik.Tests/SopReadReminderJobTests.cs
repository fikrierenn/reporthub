using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Email;
using Mosaik.Core.Logging;
using Mosaik.Core.Notification;
using Mosaik.Core.Users;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34 Faz E S-21 — SopReadReminderJob: 7 gün kala ve 1 gün kala uyarı + ReminderSentCount transit.
public class SopReadReminderJobTests
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

    private sealed class FakeNotifications : INotificationService
    {
        public List<(List<int> Ids, int? EntityId, string? NotificationType, string Title)> BulkCalls { get; } = new();

        public Task<int> CreateBulkAsync(IEnumerable<int> userIds, string entityType, int? entityId,
            string title, string? message, string? targetUrl, string? notificationType, string? createdBy)
        {
            var ids = userIds.ToList();
            BulkCalls.Add((ids, entityId, notificationType, title));
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

    private sealed class FakeEmail : IEmailService
    {
        public bool IsEnabled { get; set; }
        public List<(string To, string Subject)> Calls { get; } = new();
        public Func<string, EmailSendResult>? OnSend { get; set; }

        public Task<EmailSendResult> SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            Calls.Add((to, subject));
            var result = OnSend?.Invoke(to) ?? EmailSendResult.Ok();
            return Task.FromResult(result);
        }

        public Task<EmailBulkResult> SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class FakeUserDirectory : IActiveUserDirectory
    {
        public Dictionary<int, string> Emails { get; set; } = new();
        public Task<List<int>> GetActiveUserIdsAsync(int? firmaId = null) => Task.FromResult(new List<int>());
        public Task<Dictionary<int, string>> GetUserEmailsAsync(IEnumerable<int> userIds)
        {
            var ids = userIds.ToHashSet();
            return Task.FromResult(Emails.Where(kv => ids.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
        }
        public Task<List<Mosaik.Core.Users.ActiveUserInfo>> GetActiveUsersAsync(int? firmaId = null)
            => Task.FromResult(new List<Mosaik.Core.Users.ActiveUserInfo>());
    }

    private static (TestContext db, SopReadReminderJob job, FakeAudit audit, FakeNotifications notif, FakeEmail email, FakeUserDirectory dir) NewJob(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var audit = new FakeAudit();
        var notif = new FakeNotifications();
        var email = new FakeEmail();
        var dir = new FakeUserDirectory();
        var job = new SopReadReminderJob(db, notif, email, dir, audit, NullLogger<SopReadReminderJob>.Instance);
        return (db, job, audit, notif, email, dir);
    }

    private static async Task<int> SeedVersionAsync(TestContext db, int readDeadlineDays = 30)
    {
        var doc = new SopDocument
        {
            FirmaId = 1,
            Title = "Test SOP",
            OwnerUserId = 10,
            ReadDeadlineDays = readDeadlineDays,
            IsCompanyWide = true,
            CreatedBy = 10
        };
        db.SopDocuments.Add(doc);
        await db.SaveChangesAsync();

        var version = new SopVersion
        {
            SopDocumentId = doc.Id,
            VersionNumber = 1,
            ContentJson = "<p>x</p>",
            Status = 2,
            EffectiveDate = DateTime.UtcNow,
            CreatedBy = 10
        };
        db.SopVersions.Add(version);
        await db.SaveChangesAsync();
        return version.Id;
    }

    [Fact]
    public async Task Execute_NoPending_ReturnsZero()
    {
        var (db, job, audit, notif, _, _) = NewJob(nameof(Execute_NoPending_ReturnsZero));
        await using var _ = db;

        var result = await job.ExecuteAsync();

        Assert.Equal(0, result.Pending);
        Assert.Empty(notif.BulkCalls);
        Assert.DoesNotContain("sop_reminder_sent", audit.Events);
    }

    [Fact]
    public async Task Execute_SevenDayWindow_SendsReminderAndAdvancesCounter()
    {
        var (db, job, audit, notif, _, _) = NewJob(nameof(Execute_SevenDayWindow_SendsReminderAndAdvancesCounter));
        await using var _ = db;

        var versionId = await SeedVersionAsync(db, readDeadlineDays: 30);
        // 25 gün önce atandı → 5 gün kaldı → 7-gün penceresinde.
        db.SopReadReceipts.AddRange(
            new SopReadReceipt { SopVersionId = versionId, UserId = 11, AssignedAt = DateTime.UtcNow.AddDays(-25), ReminderSentCount = 0 },
            new SopReadReceipt { SopVersionId = versionId, UserId = 12, AssignedAt = DateTime.UtcNow.AddDays(-25), ReminderSentCount = 0 });
        await db.SaveChangesAsync();

        var result = await job.ExecuteAsync();

        Assert.Equal(2, result.Pending);
        Assert.Equal(2, result.SevenDayReminders);
        Assert.Equal(0, result.OneDayReminders);
        Assert.Single(notif.BulkCalls);
        Assert.Equal("SopReminder", notif.BulkCalls[0].NotificationType);
        Assert.Equal(2, notif.BulkCalls[0].Ids.Count);

        var refreshed = await db.SopReadReceipts.AsNoTracking().ToListAsync();
        Assert.All(refreshed, r => Assert.Equal(1, r.ReminderSentCount));
        Assert.Contains("sop_reminder_sent", audit.Events);
    }

    [Fact]
    public async Task Execute_OneDayWindow_AlreadyReminded_SendsFinalAndAdvancesCounter()
    {
        var (db, job, _, notif, _, _) = NewJob(nameof(Execute_OneDayWindow_AlreadyReminded_SendsFinalAndAdvancesCounter));
        await using var _ = db;

        var versionId = await SeedVersionAsync(db, readDeadlineDays: 30);
        // 29.5 gün önce atandı → 0.5 gün kaldı → 1-gün son uyarı.
        db.SopReadReceipts.Add(new SopReadReceipt
        {
            SopVersionId = versionId,
            UserId = 11,
            AssignedAt = DateTime.UtcNow.AddDays(-29.5),
            ReminderSentCount = 1
        });
        await db.SaveChangesAsync();

        var result = await job.ExecuteAsync();

        Assert.Equal(1, result.OneDayReminders);
        Assert.Equal(0, result.SevenDayReminders);
        Assert.Single(notif.BulkCalls);
        Assert.Equal("SopReminderFinal", notif.BulkCalls[0].NotificationType);
        Assert.Contains("Son uyarı", notif.BulkCalls[0].Title);

        var refreshed = await db.SopReadReceipts.AsNoTracking().FirstAsync();
        Assert.Equal(2, refreshed.ReminderSentCount);
    }

    [Fact]
    public async Task Execute_AlreadyConfirmed_Skipped()
    {
        var (db, job, _, notif, _, _) = NewJob(nameof(Execute_AlreadyConfirmed_Skipped));
        await using var _ = db;

        var versionId = await SeedVersionAsync(db, readDeadlineDays: 30);
        db.SopReadReceipts.Add(new SopReadReceipt
        {
            SopVersionId = versionId,
            UserId = 11,
            AssignedAt = DateTime.UtcNow.AddDays(-25),
            ConfirmedAt = DateTime.UtcNow,
            ReminderSentCount = 0
        });
        await db.SaveChangesAsync();

        var result = await job.ExecuteAsync();

        Assert.Equal(0, result.Pending);
        Assert.Empty(notif.BulkCalls);
    }

    [Fact]
    public async Task Execute_OneDayWindow_EmailEnabled_SendsEmailsToKnownAddresses()
    {
        var (db, job, _, notif, email, dir) =
            NewJob(nameof(Execute_OneDayWindow_EmailEnabled_SendsEmailsToKnownAddresses));
        await using var _ = db;

        var versionId = await SeedVersionAsync(db, readDeadlineDays: 30);
        db.SopReadReceipts.AddRange(
            new SopReadReceipt { SopVersionId = versionId, UserId = 11, AssignedAt = DateTime.UtcNow.AddDays(-29.5), ReminderSentCount = 1 },
            new SopReadReceipt { SopVersionId = versionId, UserId = 12, AssignedAt = DateTime.UtcNow.AddDays(-29.5), ReminderSentCount = 1 });
        await db.SaveChangesAsync();

        email.IsEnabled = true;
        dir.Emails[11] = "a@bkm.test";
        // UserId 12 için email yok → atlanır.

        var result = await job.ExecuteAsync();

        Assert.Equal(2, result.OneDayReminders);
        Assert.Equal(1, result.EmailsSent);
        Assert.Single(email.Calls);
        Assert.Equal("a@bkm.test", email.Calls[0].To);
        Assert.Contains("Son uyarı", email.Calls[0].Subject);
    }

    [Fact]
    public async Task Execute_OneDayWindow_EmailDisabled_SkipsEmailSilently()
    {
        var (db, job, _, _, email, dir) =
            NewJob(nameof(Execute_OneDayWindow_EmailDisabled_SkipsEmailSilently));
        await using var _ = db;

        var versionId = await SeedVersionAsync(db, readDeadlineDays: 30);
        db.SopReadReceipts.Add(new SopReadReceipt
        {
            SopVersionId = versionId,
            UserId = 11,
            AssignedAt = DateTime.UtcNow.AddDays(-29.5),
            ReminderSentCount = 1
        });
        await db.SaveChangesAsync();

        email.IsEnabled = false;
        dir.Emails[11] = "a@bkm.test";

        var result = await job.ExecuteAsync();

        Assert.Equal(1, result.OneDayReminders);
        Assert.Equal(0, result.EmailsSent);
        Assert.Empty(email.Calls);
    }

    [Fact]
    public async Task Execute_MaxReminderCount_NoFurtherDispatch()
    {
        var (db, job, _, notif, _, _) = NewJob(nameof(Execute_MaxReminderCount_NoFurtherDispatch));
        await using var _ = db;

        var versionId = await SeedVersionAsync(db, readDeadlineDays: 30);
        // count=2 zaten — overdue olsa bile yeni push yok.
        db.SopReadReceipts.Add(new SopReadReceipt
        {
            SopVersionId = versionId,
            UserId = 11,
            AssignedAt = DateTime.UtcNow.AddDays(-35),                    // overdue
            ReminderSentCount = 2
        });
        await db.SaveChangesAsync();

        var result = await job.ExecuteAsync();

        Assert.Equal(0, result.Pending);
        Assert.Empty(notif.BulkCalls);
    }
}
