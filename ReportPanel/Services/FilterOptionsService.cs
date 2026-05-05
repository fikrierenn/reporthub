using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services;

/// <summary>
/// Plan 07 Faz 3 — DB-driven filter options.
/// FilterDefinition (master) lookup + Scope'a gore kaynak:
/// - reportAccess: NativeSources registry (kod-side, type-safe EF — orn. raporKategori → ReportCategories)
/// - spInjection: FilterDefinition.DataSourceKey + OptionsQuery (admin yazar, IsSafeOptionsQuery ile dogrulanir, exec)
/// </summary>
public class FilterOptionsService
{
    private readonly ReportPanelContext _context;
    private readonly ILogger<FilterOptionsService> _logger;

    private static readonly Dictionary<string, Func<ReportPanelContext, Task<List<FilterOption>>>> NativeSources = new()
    {
        ["raporGrubu"] = async ctx => await ctx.ReportCategories
            .AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.Name)
            .Select(c => new FilterOption(c.CategoryId.ToString(), c.Name))
            .ToListAsync()
    };

    public FilterOptionsService(ReportPanelContext context, ILogger<FilterOptionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FilterOptionsResult> GetAsync(string filterKey, string? dataSourceKey = null)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
        {
            return new FilterOptionsResult(false, "FilterKey gerekli.", Array.Empty<FilterOption>());
        }

        // Plan B: aynı FilterKey birden fazla DataSource'ta olabilir; (DataSourceKey, FilterKey) composite.
        var def = await _context.FilterDefinitions.AsNoTracking()
            .FirstOrDefaultAsync(f => f.FilterKey == filterKey && f.DataSourceKey == dataSourceKey);
        if (def == null || !def.IsActive)
        {
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }

        if (def.Scope == FilterDefinition.ScopeReportAccess)
        {
            return await GetFromNativeSourceAsync(filterKey);
        }

        return await GetFromSpInjectionAsync(def);
    }

    private async Task<FilterOptionsResult> GetFromNativeSourceAsync(string filterKey)
    {
        if (!NativeSources.TryGetValue(filterKey, out var src))
        {
            _logger.LogWarning(
                "FilterDefinition '{FilterKey}' Scope=reportAccess ama NativeSources registry'de kayit yok.",
                filterKey);
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }

        try
        {
            var opts = await src(_context);
            return new FilterOptionsResult(true, null, opts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Native source query failed for {FilterKey}", filterKey);
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }
    }

    private async Task<FilterOptionsResult> GetFromSpInjectionAsync(FilterDefinition def)
    {
        if (string.IsNullOrWhiteSpace(def.DataSourceKey) || string.IsNullOrWhiteSpace(def.OptionsQuery))
        {
            _logger.LogWarning(
                "FilterDefinition '{FilterKey}' Scope=spInjection ama DataSourceKey veya OptionsQuery eksik.",
                def.FilterKey);
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }

        if (!FilterDefinitionService.IsSafeOptionsQuery(def.OptionsQuery, out var unsafeReason))
        {
            _logger.LogError(
                "FilterDefinition '{FilterKey}' OptionsQuery guvenlik kontrolunden gecmedi: {Reason}",
                def.FilterKey, unsafeReason);
            return new FilterOptionsResult(false, "Filtre sorgusu guvenlik kontrolunden gecmedi.", Array.Empty<FilterOption>());
        }

        var ds = await _context.DataSources.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DataSourceKey == def.DataSourceKey && d.IsActive);
        if (ds == null)
        {
            _logger.LogWarning(
                "FilterDefinition '{FilterKey}' DataSource '{DataSourceKey}' bulunamadi veya pasif.",
                def.FilterKey, def.DataSourceKey);
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }

        var options = new List<FilterOption>();
        try
        {
            await using var conn = new SqlConnection(ds.ConnString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(def.OptionsQuery, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                options.Add(new FilterOption(
                    reader["Value"]?.ToString() ?? "",
                    reader["Label"]?.ToString() ?? ""
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FilterOptions query failed for {FilterKey} on {DataSourceKey}",
                def.FilterKey, def.DataSourceKey);
        }

        return new FilterOptionsResult(true, null, options);
    }
}

public record FilterOptionsResult(bool Success, string? Error, IReadOnlyList<FilterOption> Options);
public record FilterOption(string Value, string Label);
