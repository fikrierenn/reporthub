using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// M-04: UserRoleSyncService idempotency + delta kontrolu.
/// EF Core InMemory provider — SQL Server'a bagimli degil, hizli.
/// </summary>
public class UserRoleSyncServiceTests
{
    private static ReportPanelContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<ReportPanelContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        var ctx = new ReportPanelContext(options);
        // Seed: 3 rol + 1 kullanici (FK constraint'i InMemory'de uygulanmaz ama veri tutarliligi icin ekleyelim)
        ctx.Roles.AddRange(
            new Role { RoleId = 1, Name = "admin", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Role { RoleId = 2, Name = "ik", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Role { RoleId = 3, Name = "mali", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        #pragma warning disable CS0618 // User.Roles [Obsolete], test icin deprecate alan bos birakiliyor
        ctx.Users.Add(new User
        {
            UserId = 10,
            Username = "alice",
            FullName = "Alice Test",
            PasswordHash = "x",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        #pragma warning restore CS0618
        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task SyncAsync_adds_all_roles_for_new_user()
    {
        await using var ctx = NewContext(nameof(SyncAsync_adds_all_roles_for_new_user));
        var svc = new UserRoleSyncService(ctx);

        await svc.SyncAsync(10, new HashSet<int> { 1, 2 });

        var persisted = await ctx.UserRoles.Where(ur => ur.UserId == 10).Select(ur => ur.RoleId).OrderBy(r => r).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, persisted);
    }

    [Fact]
    public async Task SyncAsync_replaces_old_set_with_new_delta()
    {
        await using var ctx = NewContext(nameof(SyncAsync_replaces_old_set_with_new_delta));
        var svc = new UserRoleSyncService(ctx);

        await svc.SyncAsync(10, new HashSet<int> { 1, 2 });
        await svc.SyncAsync(10, new HashSet<int> { 2, 3 });

        var persisted = await ctx.UserRoles.Where(ur => ur.UserId == 10).Select(ur => ur.RoleId).OrderBy(r => r).ToListAsync();
        Assert.Equal(new[] { 2, 3 }, persisted);
    }

    [Fact]
    public async Task SyncAsync_empty_set_removes_all_user_role_rows()
    {
        await using var ctx = NewContext(nameof(SyncAsync_empty_set_removes_all_user_role_rows));
        var svc = new UserRoleSyncService(ctx);

        await svc.SyncAsync(10, new HashSet<int> { 1, 2, 3 });
        Assert.Equal(3, await ctx.UserRoles.CountAsync(ur => ur.UserId == 10));

        await svc.SyncAsync(10, new HashSet<int>());

        Assert.Equal(0, await ctx.UserRoles.CountAsync(ur => ur.UserId == 10));
    }

    [Fact]
    public async Task SyncAsync_idempotent_same_set_produces_same_state()
    {
        await using var ctx = NewContext(nameof(SyncAsync_idempotent_same_set_produces_same_state));
        var svc = new UserRoleSyncService(ctx);

        await svc.SyncAsync(10, new HashSet<int> { 1, 2 });
        await svc.SyncAsync(10, new HashSet<int> { 1, 2 });
        await svc.SyncAsync(10, new HashSet<int> { 1, 2 });

        var ids = await ctx.UserRoles.Where(ur => ur.UserId == 10).Select(ur => ur.RoleId).OrderBy(r => r).ToListAsync();
        Assert.Equal(new[] { 1, 2 }, ids);
    }

    [Fact]
    public async Task SyncAsync_does_not_touch_other_users_rows()
    {
        await using var ctx = NewContext(nameof(SyncAsync_does_not_touch_other_users_rows));
        var svc = new UserRoleSyncService(ctx);

        // Baska kullanicinin rolleri onceden var
        ctx.UserRoles.Add(new UserRole { UserId = 20, RoleId = 1, CreatedAt = DateTime.UtcNow });
        ctx.UserRoles.Add(new UserRole { UserId = 20, RoleId = 3, CreatedAt = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        await svc.SyncAsync(10, new HashSet<int> { 2 });

        var other = await ctx.UserRoles.Where(ur => ur.UserId == 20).Select(ur => ur.RoleId).OrderBy(r => r).ToListAsync();
        Assert.Equal(new[] { 1, 3 }, other);
    }
}
