using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ReportPanel.Models;
using ReportPanel.Services;

namespace ReportPanel.Tests;

/// <summary>
/// Plan 07 Faz 3 — FilterOptionsService DB-driven davranis testleri.
/// reportAccess (NativeSources) yolu InMemory ile dogrulanir;
/// spInjection happy-path (gercek SqlConnection) integration kapsami — burada exec edilemeyen senaryolar.
/// </summary>
public class FilterOptionsServiceTests
{
    private static ReportPanelContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<ReportPanelContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new ReportPanelContext(options);
    }

    private static FilterOptionsService NewService(ReportPanelContext ctx)
        => new(ctx, NullLogger<FilterOptionsService>.Instance);

    [Fact]
    public async Task GetAsync_empty_filterKey_returns_failure()
    {
        await using var ctx = NewContext(nameof(GetAsync_empty_filterKey_returns_failure));
        var svc = NewService(ctx);

        var result = await svc.GetAsync("");

        Assert.False(result.Success);
        Assert.Equal("FilterKey gerekli.", result.Error);
        Assert.Empty(result.Options);
    }

    [Fact]
    public async Task GetAsync_unknown_filterKey_returns_empty_list()
    {
        await using var ctx = NewContext(nameof(GetAsync_unknown_filterKey_returns_empty_list));
        var svc = NewService(ctx);

        var result = await svc.GetAsync("nonexistent");

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.Empty(result.Options);
    }

    [Fact]
    public async Task GetAsync_inactive_definition_returns_empty()
    {
        await using var ctx = NewContext(nameof(GetAsync_inactive_definition_returns_empty));
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "raporKategori",
            Label = "Rapor Kategorisi",
            Scope = FilterDefinition.ScopeReportAccess,
            IsActive = false
        });
        await ctx.SaveChangesAsync();
        var svc = NewService(ctx);

        var result = await svc.GetAsync("raporKategori");

        Assert.True(result.Success);
        Assert.Empty(result.Options);
    }

    [Fact]
    public async Task GetAsync_reportAccess_native_source_returns_active_categories_ordered()
    {
        await using var ctx = NewContext(nameof(GetAsync_reportAccess_native_source_returns_active_categories_ordered));
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "raporGrubu",
            Label = "Rapor Grubu",
            Scope = FilterDefinition.ScopeReportAccess,
            IsActive = true
        });
        ctx.ReportGroups.AddRange(
            new ReportGroup { GroupId = 1, Name = "Zenith", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReportGroup { GroupId = 2, Name = "Alpha", IsActive = true, CreatedAt = DateTime.UtcNow },
            new ReportGroup { GroupId = 3, Name = "Beta_Inactive", IsActive = false, CreatedAt = DateTime.UtcNow }
        );
        await ctx.SaveChangesAsync();
        var svc = NewService(ctx);

        var result = await svc.GetAsync("raporGrubu");

        Assert.True(result.Success);
        Assert.Equal(2, result.Options.Count);
        Assert.Equal("Alpha", result.Options[0].Label);
        Assert.Equal("2", result.Options[0].Value);
        Assert.Equal("Zenith", result.Options[1].Label);
    }

    [Fact]
    public async Task GetAsync_reportAccess_no_native_source_registered_returns_empty()
    {
        await using var ctx = NewContext(nameof(GetAsync_reportAccess_no_native_source_registered_returns_empty));
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "yetkisizNative",
            Label = "Yetkisiz Native",
            Scope = FilterDefinition.ScopeReportAccess,
            IsActive = true
        });
        await ctx.SaveChangesAsync();
        var svc = NewService(ctx);

        var result = await svc.GetAsync("yetkisizNative");

        Assert.True(result.Success);
        Assert.Empty(result.Options);
    }

    [Fact]
    public async Task GetAsync_spInjection_unsafe_query_rejected()
    {
        await using var ctx = NewContext(nameof(GetAsync_spInjection_unsafe_query_rejected));
        ctx.DataSources.Add(new DataSource
        {
            DataSourceKey = "PDKS",
            Title = "PDKS",
            ConnString = "Server=.;Database=Fake;Integrated Security=true",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "evil",
            Label = "Evil",
            Scope = FilterDefinition.ScopeSpInjection,
            DataSourceKey = "PDKS",
            OptionsQuery = "SELECT 1 AS Value, 'x' AS Label; DELETE FROM Users",
            IsActive = true
        });
        await ctx.SaveChangesAsync();
        var svc = NewService(ctx);

        var result = await svc.GetAsync("evil", "PDKS");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
        Assert.Empty(result.Options);
    }

    [Fact]
    public async Task GetAsync_spInjection_missing_datasource_returns_empty()
    {
        await using var ctx = NewContext(nameof(GetAsync_spInjection_missing_datasource_returns_empty));
        ctx.FilterDefinitions.Add(new FilterDefinition
        {
            FilterKey = "sube",
            Label = "Şube",
            Scope = FilterDefinition.ScopeSpInjection,
            DataSourceKey = null,
            OptionsQuery = null,
            IsActive = true
        });
        await ctx.SaveChangesAsync();
        var svc = NewService(ctx);

        var result = await svc.GetAsync("sube");

        Assert.True(result.Success);
        Assert.Empty(result.Options);
    }
}
