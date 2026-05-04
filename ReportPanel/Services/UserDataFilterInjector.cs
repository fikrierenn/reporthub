using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services;

/// <summary>
/// Multi-tenant veri filtresi enjeksiyon servisi.
/// Kullanıcının UserDataFilter kayıtlarına göre SP parametrelerine `@FilterKey_Filtre`
/// adında parametre ekler. SP tarafında:
///   WHERE (@sube_Filtre IS NULL OR Sube IN (SELECT value FROM STRING_SPLIT(@sube_Filtre, ',')))
/// G-03 (UserDataFilterValidator whitelist + regex) burada uygulanır; reject'ler AuditLog'a
/// `user_filter_rejected` event'i ile yazılır (multi-tenant ihlal sinyali).
///
/// ReportsController.InjectUserDataFilters'tan çıkarıldı (M-13 R6.2, 28 Nisan 2026).
/// </summary>
public class UserDataFilterInjector
{
    private readonly ReportPanelContext _context;
    private readonly AuditLogService _auditLog;

    public UserDataFilterInjector(ReportPanelContext context, AuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    public async Task InjectAsync(
        List<SqlParameter> parameters,
        int? userId,
        int reportId,
        string dataSourceKey)
    {
        if (userId == null) return;

        // Plan 07 Faz 4: Aktif FilterDefinition'lar — deny-by-default check icin.
        var activeKeys = await _context.FilterDefinitions.AsNoTracking()
            .Where(f => f.IsActive)
            .Select(f => f.FilterKey)
            .ToListAsync();

        // Kullanıcının tüm filtrelerini çek (bu rapor + genel).
        var filters = await _context.UserDataFilters
            .Where(f => f.UserId == userId.Value
                && (f.ReportId == null || f.ReportId == reportId)
                && (f.DataSourceKey == null || f.DataSourceKey == dataSourceKey))
            .ToListAsync();

        // Plan 07 Faz 4: Aktif her FilterDefinition icin kullanicinin en az 1 kaydi
        // (concrete veya '*') olmali; yoksa caller'i 403 ile durdur. Backfill (Migration 20)
        // mevcut user'lara her aktif key icin '*' kaydi ekledi — atlanan yeni user'lar deny.
        foreach (var defKey in activeKeys)
        {
            var hasRecord = filters.Any(f =>
                string.Equals(f.FilterKey, defKey, StringComparison.OrdinalIgnoreCase));
            if (!hasRecord)
            {
                throw new UserDataFilterDeniedException(defKey, userId);
            }
        }

        if (filters.Count == 0) return; // Hic aktif FilterDefinition + 0 filter = unrestricted

        // Plan 07 Faz 4: '*' (FilterDefinition.ValueAll) "Hepsi" anlami — parametre eklenmiyor,
        // SP'de WHERE @p IS NULL dali calisir. Validator'a girmiyor (FilterValueRegex '*' kabul etmez).
        var concreteFilters = filters
            .Where(f => f.FilterValue != FilterDefinition.ValueAll)
            .ToList();

        // G-03: UserDataFilterValidator (whitelist + regex).
        var validFilters = concreteFilters
            .Where(f => UserDataFilterValidator.IsValid(f.FilterKey, f.FilterValue))
            .ToList();

        // Reject edilenleri audit log'a yaz (multi-tenant ihlal sinyali olabilir).
        var rejected = concreteFilters.Except(validFilters).ToList();
        foreach (var r in rejected)
        {
            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "user_filter_rejected",
                TargetType = "user_data_filter",
                TargetKey = r.FilterId.ToString(),
                Description = $"G-03 whitelist rejected filter (key='{r.FilterKey}', valueLen={r.FilterValue?.Length ?? 0})"
            });
        }

        if (!validFilters.Any()) return;

        // FilterKey bazında grupla ve virgülle birleştir.
        var grouped = validFilters
            .GroupBy(f => f.FilterKey.ToLowerInvariant())
            .ToDictionary(
                g => g.Key,
                g => string.Join(",", g.Select(f => f.FilterValue)));

        // Plan 07 Faz 5b: 'sube' icin SubeMapping translate (canonical SubeId → DataSource ExternalCode).
        // Mapping eksikse o sube o sistemin parametresine girmez (sessiz drop).
        var subeKey = grouped.Keys.FirstOrDefault(k => string.Equals(k, "sube", StringComparison.OrdinalIgnoreCase));
        Dictionary<int, string>? subeIdToExternal = null;
        if (subeKey != null)
        {
            var subeIds = grouped[subeKey].Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

            if (subeIds.Count > 0)
            {
                subeIdToExternal = await _context.SubeMappings.AsNoTracking()
                    .Where(m => subeIds.Contains(m.SubeId) && m.DataSourceKey == dataSourceKey)
                    .ToDictionaryAsync(m => m.SubeId, m => m.ExternalCode);
            }
        }

        // Her filtre grubu için @key_Filtre parametresi ekle.
        // Zaten aynı isimde parametre varsa (kullanıcı formdan girmiş) ekleme.
        foreach (var kvp in grouped)
        {
            var paramName = $"@{kvp.Key}_Filtre";
            if (parameters.Any(p => p.ParameterName.Equals(paramName, StringComparison.OrdinalIgnoreCase)))
                continue;

            string paramValue = kvp.Value;

            // sube ozel: canonical SubeId → ExternalCode translate, eksik mapping drop
            if (string.Equals(kvp.Key, "sube", StringComparison.OrdinalIgnoreCase))
            {
                if (subeIdToExternal == null || subeIdToExternal.Count == 0)
                {
                    continue; // hicbir mapping yok, bu sistem icin sube kisitlamasi yok = sessiz drop
                }

                var translated = kvp.Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                    .Where(id => id.HasValue && subeIdToExternal.ContainsKey(id!.Value))
                    .Select(id => subeIdToExternal[id!.Value])
                    .Distinct()
                    .ToList();

                if (translated.Count == 0) continue; // tum SubeId'ler bu DataSource'ta drop edildi
                paramValue = string.Join(",", translated);
            }

            parameters.Add(new SqlParameter(paramName, SqlDbType.NVarChar, 500)
            {
                Value = paramValue
            });
        }
    }
}
