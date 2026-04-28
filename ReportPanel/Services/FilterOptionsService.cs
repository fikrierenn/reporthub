using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services;

/// <summary>
/// Kullanıcı veri filtresi seçenekleri (UserDataFilter UI dropdown'ları).
/// Whitelist'li filterKey için (sube/bolum vb.) DataSource üzerinden distinct değerleri listeler.
/// AdminController.FilterOptions action'ından çıkarıldı (M-13 R4.1, 28 Nisan 2026).
/// </summary>
public class FilterOptionsService
{
    private readonly ReportPanelContext _context;
    private readonly ILogger<FilterOptionsService> _logger;

    public FilterOptionsService(ReportPanelContext context, ILogger<FilterOptionsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<FilterOptionsResult> GetAsync(string filterKey, string? dataSourceKey)
    {
        if (string.IsNullOrWhiteSpace(filterKey))
        {
            return new FilterOptionsResult(false, "FilterKey gerekli.", Array.Empty<FilterOption>());
        }

        // Data source bul — belirtilmişse onu, yoksa ilk aktif DS'i.
        var dsKey = dataSourceKey;
        if (string.IsNullOrWhiteSpace(dsKey))
        {
            var first = await _context.DataSources.AsNoTracking()
                .FirstOrDefaultAsync(d => d.IsActive);
            dsKey = first?.DataSourceKey;
        }

        var ds = await _context.DataSources.AsNoTracking()
            .FirstOrDefaultAsync(d => d.DataSourceKey == dsKey && d.IsActive);
        if (ds == null)
        {
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }

        // Whitelist'li filterKey → SQL. Bilinmeyen key'lerde boş liste (silent reject).
        string sql = filterKey.ToLowerInvariant() switch
        {
            "sube" => "SELECT CAST(SubeNo AS varchar(10)) AS Value, SubeAd AS Label FROM vrd.SubeListe ORDER BY SubeAd",
            "bolum" => "SELECT DISTINCT Bolum AS Value, Bolum AS Label FROM vrd.VardiyaDetay WHERE Bolum IS NOT NULL AND Bolum <> '' ORDER BY Bolum",
            _ => ""
        };

        if (string.IsNullOrEmpty(sql))
        {
            return new FilterOptionsResult(true, null, Array.Empty<FilterOption>());
        }

        var options = new List<FilterOption>();
        try
        {
            await using var conn = new SqlConnection(ds.ConnString);
            await conn.OpenAsync();
            await using var cmd = new SqlCommand(sql, conn);
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
            _logger.LogWarning(ex, "FilterOptions query failed for {FilterKey} on {DataSourceKey}", filterKey, dsKey);
            // Connection hatası — boş liste dön (UI bozulmasın).
        }

        return new FilterOptionsResult(true, null, options);
    }
}

public record FilterOptionsResult(bool Success, string? Error, IReadOnlyList<FilterOption> Options);
public record FilterOption(string Value, string Label);
