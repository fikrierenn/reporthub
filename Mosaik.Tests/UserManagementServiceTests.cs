using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Tests;

/// <summary>
/// Plan 14 Faz A — UserManagementService.SyncDataFiltersAsync diff audit + no-change detection.
/// Audit log: added/removed listesi JSON, no-change durumunda audit yazilmaz.
/// </summary>
public class UserManagementServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        var ctx = new MosaikContext(options);
        ctx.Roles.Add(new Role { RoleId = 1, Name = "admin", IsActive = true, CreatedAt = DateTime.UtcNow });
        #pragma warning disable CS0618
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

    private static UserManagementService NewService(MosaikContext ctx)
    {
        var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var audit = new AuditLogService(ctx, httpAccessor, NullLogger<AuditLogService>.Instance);
        var roleSync = new UserRoleSyncService(ctx);
        return new UserManagementService(ctx, audit, roleSync);
    }

    private static UserFormInput Input(int roleId, params (string key, string value, string? ds)[] filters) =>
        new UserFormInput(
            Username: "alice",
            FullName: "Alice Test",
            Email: null,
            IsAdUser: false,
            IsActive: true,
            Password: "p1",
            FirmaIds: null,
            SelectedRoleIds: new HashSet<int> { roleId },
            DataFilters: filters.Select(f => new UserFilterInput(f.key, f.value, f.ds)).ToList());

    [Fact]
    public async Task SyncDataFilters_AddedAndRemoved_LogsDiffEvent()
    {
        await using var ctx = NewContext(nameof(SyncDataFilters_AddedAndRemoved_LogsDiffEvent));
        var svc = NewService(ctx);

        // Onceki durum: 2 filter (sube=*, urunKategori=KITAP)
        await svc.UpdateAsync(10, Input(1, ("sube", "*", "PDKS"), ("urunKategori", "KITAP", "DER")));
        var auditCountBefore = await ctx.AuditLogs.CountAsync(a => a.EventType == "user_data_filter_sync");

        // Yeni durum: sube=1,4477 (degisti) + urunKategori KALDIRILDI + raporGrubu=* (eklendi)
        await svc.UpdateAsync(10, Input(1, ("sube", "1,4477", "PDKS"), ("raporGrubu", "*", null)));

        var diffEvents = await ctx.AuditLogs
            .Where(a => a.EventType == "user_data_filter_sync")
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
        Assert.Equal(auditCountBefore + 1, diffEvents.Count);

        var lastEvent = diffEvents.Last();
        Assert.Equal("user", lastEvent.TargetType);
        Assert.Equal("10", lastEvent.TargetKey);
        Assert.Contains("\"added\"", lastEvent.NewValuesJson);
        Assert.Contains("\"removed\"", lastEvent.NewValuesJson);
        Assert.Contains("urunKategori", lastEvent.NewValuesJson); // removed icinde
        Assert.Contains("raporGrubu", lastEvent.NewValuesJson);   // added icinde
        Assert.Contains("1,4477", lastEvent.NewValuesJson);        // added (sube degisti)
    }

    [Fact]
    public async Task SyncDataFilters_NoChange_SkipsAuditLog()
    {
        await using var ctx = NewContext(nameof(SyncDataFilters_NoChange_SkipsAuditLog));
        var svc = NewService(ctx);

        await svc.UpdateAsync(10, Input(1, ("sube", "*", "PDKS")));
        var firstSyncCount = await ctx.AuditLogs.CountAsync(a => a.EventType == "user_data_filter_sync");

        // Ayni filtre tekrar — diff bos olmali, audit yazilmamali
        await svc.UpdateAsync(10, Input(1, ("sube", "*", "PDKS")));
        var secondSyncCount = await ctx.AuditLogs.CountAsync(a => a.EventType == "user_data_filter_sync");

        Assert.Equal(firstSyncCount, secondSyncCount);
    }

    [Fact]
    public async Task SyncDataFilters_DataSourceKeyChange_TreatedAsAddedAndRemoved()
    {
        await using var ctx = NewContext(nameof(SyncDataFilters_DataSourceKeyChange_TreatedAsAddedAndRemoved));
        var svc = NewService(ctx);

        await svc.UpdateAsync(10, Input(1, ("sube", "*", "PDKS")));
        var initial = await ctx.AuditLogs.CountAsync(a => a.EventType == "user_data_filter_sync");

        // Ayni Key+Value ama DataSourceKey degisti — set tuple'da DataSourceKey de var, removed+added cikar
        await svc.UpdateAsync(10, Input(1, ("sube", "*", "DER")));
        var after = await ctx.AuditLogs
            .Where(a => a.EventType == "user_data_filter_sync")
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();

        Assert.Equal(initial + 1, after.Count);
        Assert.Contains("PDKS", after.Last().NewValuesJson); // removed
        Assert.Contains("DER", after.Last().NewValuesJson);  // added
    }

    [Fact]
    public async Task SyncDataFilters_EmptyValueFilters_AreIgnored()
    {
        await using var ctx = NewContext(nameof(SyncDataFilters_EmptyValueFilters_AreIgnored));
        var svc = NewService(ctx);

        // Bos value icermeyen filtreler skip edilir, sadece dolu olan eklenir
        await svc.UpdateAsync(10, Input(1, ("sube", "1", "PDKS"), ("bos", "", null)));

        var stored = await ctx.UserDataFilters.Where(f => f.UserId == 10).ToListAsync();
        Assert.Single(stored);
        Assert.Equal("sube", stored[0].FilterKey);
    }
}
