using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Tests;

/// <summary>
/// Plan 45 F-1 — Delete audit'inde ConnString sızmamalı (audit log read access olan
/// herkese connection string leak'lemez). Edit path zaten temiz, delete'i de doğrula.
/// </summary>
public class DataSourceManagementServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        var ctx = new MosaikContext(options);
        ctx.DataSources.Add(new DataSource
        {
            DataSourceKey = "PDKS",
            Title = "PDKS DB",
            ConnString = "Server=secret-host;User=sa;Password=Sup3rS3cret!;Database=PDKS",
            IsActive = true
        });
        ctx.SaveChanges();
        return ctx;
    }

    private static (DataSourceManagementService svc, MosaikContext ctx) NewService(string name)
    {
        var ctx = NewContext(name);
        var httpAccessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext() };
        var audit = new AuditLogService(ctx, httpAccessor, NullLogger<AuditLogService>.Instance);
        var svc = new DataSourceManagementService(ctx, audit);
        return (svc, ctx);
    }

    [Fact]
    public async Task DeleteAsync_AuditOldValues_DoesNotLeakConnString()
    {
        var (svc, ctx) = NewService(nameof(DeleteAsync_AuditOldValues_DoesNotLeakConnString));
        await using var _ = ctx;

        var result = await svc.DeleteAsync("PDKS");
        Assert.True(result.Success, result.Message);

        var auditEntry = await ctx.AuditLogs
            .Where(a => a.EventType == "datasource_delete" && a.TargetKey == "PDKS")
            .SingleAsync();

        Assert.NotNull(auditEntry.OldValuesJson);
        Assert.DoesNotContain("Sup3rS3cret", auditEntry.OldValuesJson);
        Assert.DoesNotContain("ConnString", auditEntry.OldValuesJson);
        Assert.DoesNotContain("secret-host", auditEntry.OldValuesJson);
    }
}
