using Microsoft.EntityFrameworkCore;
using Mosaik.Core.DataScope;
using Mosaik.Models;

namespace Mosaik.Tests;

// Plan 14 Faz C1 — IUserDataScope implementasyonları (SpInjectionScope + ReportAccessScope).
public class UserDataScopeTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static UserDataFilter Filter(int userId, string key, string value, string? ds = null) =>
        new()
        {
            UserId = userId,
            FilterKey = key,
            FilterValue = value,
            DataSourceKey = ds,
            CreatedAt = DateTime.UtcNow
        };

    // SpInjectionScope -----------------------------------------------------

    [Fact]
    public async Task SpInjection_NoFilter_DenyByDefault()
    {
        await using var ctx = NewContext(nameof(SpInjection_NoFilter_DenyByDefault));
        var scope = new SpInjectionScope(ctx);

        Assert.False(await scope.HasAccessAsync(10, "PDKS", "1"));
        Assert.True(scope.RequiresExplicitGrant);
        Assert.Equal("spInjection", scope.Scope);
    }

    [Fact]
    public async Task SpInjection_StarFilter_AllowsAny()
    {
        await using var ctx = NewContext(nameof(SpInjection_StarFilter_AllowsAny));
        ctx.UserDataFilters.Add(Filter(10, "sube", "*", "PDKS"));
        await ctx.SaveChangesAsync();

        var scope = new SpInjectionScope(ctx);
        Assert.True(await scope.HasAccessAsync(10, "PDKS", "1"));
        Assert.True(await scope.HasAccessAsync(10, "PDKS", "9999"));
    }

    [Fact]
    public async Task SpInjection_CsvFilter_AllowsListedValues()
    {
        await using var ctx = NewContext(nameof(SpInjection_CsvFilter_AllowsListedValues));
        ctx.UserDataFilters.Add(Filter(10, "sube", "1,4477", "PDKS"));
        await ctx.SaveChangesAsync();

        var scope = new SpInjectionScope(ctx);
        Assert.True(await scope.HasAccessAsync(10, "PDKS", "1"));
        Assert.True(await scope.HasAccessAsync(10, "PDKS", "4477"));
        Assert.False(await scope.HasAccessAsync(10, "PDKS", "9999"));
    }

    [Fact]
    public async Task SpInjection_DataSourceMismatch_DeniesAccess()
    {
        await using var ctx = NewContext(nameof(SpInjection_DataSourceMismatch_DeniesAccess));
        ctx.UserDataFilters.Add(Filter(10, "sube", "1", "PDKS"));
        await ctx.SaveChangesAsync();

        var scope = new SpInjectionScope(ctx);
        // PDKS sube ID'si IK için geçerli değil (BKM heterojenliği)
        Assert.False(await scope.HasAccessAsync(10, "IK", "1"));
    }

    [Fact]
    public async Task SpInjection_ListAccessibleValues_ReturnsExpanded()
    {
        await using var ctx = NewContext(nameof(SpInjection_ListAccessibleValues_ReturnsExpanded));
        ctx.UserDataFilters.Add(Filter(10, "sube", "1,4477", "PDKS"));
        await ctx.SaveChangesAsync();

        var scope = new SpInjectionScope(ctx);
        var values = await scope.ListAccessibleValuesAsync(10, "PDKS");
        Assert.Equal(2, values.Count);
        Assert.Contains("1", values);
        Assert.Contains("4477", values);
    }

    [Fact]
    public async Task SpInjection_ListAccessibleValues_StarReturnsStarOnly()
    {
        await using var ctx = NewContext(nameof(SpInjection_ListAccessibleValues_StarReturnsStarOnly));
        ctx.UserDataFilters.Add(Filter(10, "sube", "*", "PDKS"));
        await ctx.SaveChangesAsync();

        var scope = new SpInjectionScope(ctx);
        var values = await scope.ListAccessibleValuesAsync(10, "PDKS");
        Assert.Single(values);
        Assert.Equal("*", values[0]);
    }

    // ReportAccessScope ----------------------------------------------------

    [Fact]
    public async Task ReportAccess_NoFilter_DenyByDefault()
    {
        await using var ctx = NewContext(nameof(ReportAccess_NoFilter_DenyByDefault));
        var scope = new ReportAccessScope(ctx);

        Assert.False(await scope.HasAccessAsync(10, null, "5"));
        Assert.Equal("reportAccess", scope.Scope);
        Assert.True(scope.RequiresExplicitGrant);
    }

    [Fact]
    public async Task ReportAccess_StarFilter_AllowsAny()
    {
        await using var ctx = NewContext(nameof(ReportAccess_StarFilter_AllowsAny));
        ctx.UserDataFilters.Add(Filter(10, "raporGrubu", "*"));
        await ctx.SaveChangesAsync();

        var scope = new ReportAccessScope(ctx);
        Assert.True(await scope.HasAccessAsync(10, null, "5"));
        Assert.True(await scope.HasAccessAsync(10, null, "999"));
    }

    [Fact]
    public async Task ReportAccess_ConcreteIds_AllowsOnlyListed()
    {
        await using var ctx = NewContext(nameof(ReportAccess_ConcreteIds_AllowsOnlyListed));
        ctx.UserDataFilters.AddRange(
            Filter(10, "raporGrubu", "5"),
            Filter(10, "raporGrubu", "7"));
        await ctx.SaveChangesAsync();

        var scope = new ReportAccessScope(ctx);
        Assert.True(await scope.HasAccessAsync(10, null, "5"));
        Assert.True(await scope.HasAccessAsync(10, null, "7"));
        Assert.False(await scope.HasAccessAsync(10, null, "9"));
    }

    [Fact]
    public async Task ReportAccess_OtherFilterKey_Ignored()
    {
        await using var ctx = NewContext(nameof(ReportAccess_OtherFilterKey_Ignored));
        ctx.UserDataFilters.Add(Filter(10, "sube", "*", "PDKS"));
        await ctx.SaveChangesAsync();

        var scope = new ReportAccessScope(ctx);
        // raporGrubu kaydı yok — sube kaydı raporGrubu erişimi vermez
        Assert.False(await scope.HasAccessAsync(10, null, "5"));
    }

    // DataScopeRegistry ----------------------------------------------------

    [Fact]
    public void Registry_ResolvesScopeByKey()
    {
        var ctx = NewContext("registry");
        var scopes = new IUserDataScope[]
        {
            new SpInjectionScope(ctx),
            new ReportAccessScope(ctx)
        };
        var registry = new DataScopeRegistry(scopes);

        Assert.NotNull(registry.Get("spInjection"));
        Assert.NotNull(registry.Get("reportAccess"));
        Assert.NotNull(registry.Get("REPORTACCESS")); // case-insensitive
        Assert.Null(registry.Get("nonexistent"));
        Assert.Equal(2, registry.All.Count());
        Assert.Equal(2, registry.RequireExplicitGrant.Count());
    }
}
