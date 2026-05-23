using Microsoft.EntityFrameworkCore;
using Mosaik.Modules.SOP.Entities;
using Mosaik.Modules.SOP.Services;

namespace Mosaik.Tests;

// Plan 34.1 Faz 3 A-18 — 20 soru/saat/user rate limit + admin bypass.
public class SopRateLimitGuardTests
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

    private static (TestContext db, SopRateLimitGuard guard) NewGuard(string name)
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(name + "_" + Guid.NewGuid())
            .Options;
        var db = new TestContext(options);
        var guard = new SopRateLimitGuard(db);
        return (db, guard);
    }

    private static async Task SeedConversationsAsync(TestContext db, int userId, int count, TimeSpan offsetFromNow)
    {
        var baseTime = DateTime.UtcNow.Subtract(offsetFromNow);
        for (int i = 0; i < count; i++)
        {
            db.SopAiConversations.Add(new SopAiConversation
            {
                UserId = userId,
                Question = $"q{i}",
                Answer = "a",
                CreatedAt = baseTime.AddSeconds(i)
            });
        }
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Check_AdminBypass_AlwaysCanAsk()
    {
        var (db, guard) = NewGuard(nameof(Check_AdminBypass_AlwaysCanAsk));
        await using var _ = db;

        await SeedConversationsAsync(db, userId: 1, count: 100, offsetFromNow: TimeSpan.FromMinutes(10));

        var result = await guard.CheckAsync(userId: 1, isAdmin: true);

        Assert.True(result.CanAsk);
        Assert.Equal(int.MaxValue, result.RemainingQuota);
        Assert.Equal(0, result.UsedInWindow);
    }

    [Fact]
    public async Task Check_NoHistory_FullQuotaAvailable()
    {
        var (db, guard) = NewGuard(nameof(Check_NoHistory_FullQuotaAvailable));
        await using var _ = db;

        var result = await guard.CheckAsync(userId: 1, isAdmin: false);

        Assert.True(result.CanAsk);
        Assert.Equal(SopRateLimitGuard.HourlyLimit, result.RemainingQuota);
        Assert.Equal(0, result.UsedInWindow);
        Assert.Null(result.WindowResetAt);
    }

    [Fact]
    public async Task Check_UnderLimit_ReturnsCanAskWithRemaining()
    {
        var (db, guard) = NewGuard(nameof(Check_UnderLimit_ReturnsCanAskWithRemaining));
        await using var _ = db;

        await SeedConversationsAsync(db, userId: 1, count: 5, offsetFromNow: TimeSpan.FromMinutes(30));

        var result = await guard.CheckAsync(userId: 1, isAdmin: false);

        Assert.True(result.CanAsk);
        Assert.Equal(15, result.RemainingQuota);
        Assert.Equal(5, result.UsedInWindow);
        Assert.NotNull(result.WindowResetAt);
    }

    [Fact]
    public async Task Check_AtLimit_Denied()
    {
        var (db, guard) = NewGuard(nameof(Check_AtLimit_Denied));
        await using var _ = db;

        await SeedConversationsAsync(db, userId: 1, count: SopRateLimitGuard.HourlyLimit, offsetFromNow: TimeSpan.FromMinutes(20));

        var result = await guard.CheckAsync(userId: 1, isAdmin: false);

        Assert.False(result.CanAsk);
        Assert.Equal(0, result.RemainingQuota);
        Assert.Equal(SopRateLimitGuard.HourlyLimit, result.UsedInWindow);
    }

    [Fact]
    public async Task Check_OldRecords_OutsideWindow_NotCounted()
    {
        var (db, guard) = NewGuard(nameof(Check_OldRecords_OutsideWindow_NotCounted));
        await using var _ = db;

        // 2 saat önceki kayıtlar pencere dışı.
        await SeedConversationsAsync(db, userId: 1, count: 25, offsetFromNow: TimeSpan.FromHours(2));

        var result = await guard.CheckAsync(userId: 1, isAdmin: false);

        Assert.True(result.CanAsk);
        Assert.Equal(0, result.UsedInWindow);
        Assert.Equal(SopRateLimitGuard.HourlyLimit, result.RemainingQuota);
    }

    [Fact]
    public async Task Check_DifferentUsers_Independent()
    {
        var (db, guard) = NewGuard(nameof(Check_DifferentUsers_Independent));
        await using var _ = db;

        await SeedConversationsAsync(db, userId: 1, count: 20, offsetFromNow: TimeSpan.FromMinutes(10));
        // User 2 hiç sorgu sormamış.

        var u1 = await guard.CheckAsync(userId: 1, isAdmin: false);
        var u2 = await guard.CheckAsync(userId: 2, isAdmin: false);

        Assert.False(u1.CanAsk);
        Assert.True(u2.CanAsk);
        Assert.Equal(SopRateLimitGuard.HourlyLimit, u2.RemainingQuota);
    }
}
