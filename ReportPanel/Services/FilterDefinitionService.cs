using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    /// <summary>
    /// Plan 07 Faz 2 — FilterDefinition (master tablo) CRUD.
    /// Migration 20'de seed edilen 'sube' (spInjection) + 'raporKategori' (reportAccess) kayitlari uzerine genislet.
    /// OptionsQuery validation: SELECT-only (EXEC/INSERT/DELETE/DROP/UPDATE/MERGE/ALTER/TRUNCATE reddedilir).
    /// </summary>
    public class FilterDefinitionService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        private static readonly Regex FilterKeyPattern = new("^[a-zA-Z][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        private static readonly Regex DangerousSqlPattern = new(
            @"\b(EXEC|EXECUTE|INSERT|UPDATE|DELETE|DROP|MERGE|ALTER|TRUNCATE|GRANT|REVOKE|CREATE)\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public FilterDefinitionService(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<List<FilterDefinition>> ListAsync(bool includeInactive = true)
        {
            var query = _context.FilterDefinitions.AsNoTracking();
            if (!includeInactive) query = query.Where(f => f.IsActive);
            return await query
                .OrderBy(f => f.DisplayOrder)
                .ThenBy(f => f.Label)
                .ToListAsync();
        }

        public async Task<List<FilterDefinition>> GetActiveAsync()
            => await ListAsync(includeInactive: false);

        public async Task<FilterDefinition?> GetByIdAsync(int id)
            => await _context.FilterDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(f => f.FilterDefinitionId == id);

        public async Task<FilterDefinition?> GetByKeyAsync(string filterKey)
            => await _context.FilterDefinitions.AsNoTracking()
                .FirstOrDefaultAsync(f => f.FilterKey == filterKey);

        public async Task<AdminOperationResult> CreateAsync(
            string? filterKey, string? label, string? scope,
            string? dataSourceKey, string? optionsQuery,
            bool isActive, int displayOrder)
        {
            var validation = await ValidateAsync(null, filterKey, label, scope, dataSourceKey, optionsQuery);
            if (!validation.Success) return validation;

            var entity = new FilterDefinition
            {
                FilterKey = filterKey!.Trim(),
                Label = label!.Trim(),
                Scope = scope!.Trim(),
                DataSourceKey = string.IsNullOrWhiteSpace(dataSourceKey) ? null : dataSourceKey.Trim(),
                OptionsQuery = string.IsNullOrWhiteSpace(optionsQuery) ? null : optionsQuery.Trim(),
                IsActive = isActive,
                DisplayOrder = displayOrder
            };

            _context.FilterDefinitions.Add(entity);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "filter_definition_create",
                TargetType = "filter_definition",
                TargetKey = entity.FilterDefinitionId.ToString(),
                Description = $"FilterDefinition '{entity.FilterKey}' created",
                NewValuesJson = AuditLogService.ToJson(new { entity.FilterDefinitionId, entity.FilterKey, entity.Label, entity.Scope, entity.DataSourceKey, entity.IsActive, entity.DisplayOrder }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Filtre tanimi eklendi.");
        }

        public async Task<AdminOperationResult> UpdateAsync(
            int filterDefinitionId, string? filterKey, string? label, string? scope,
            string? dataSourceKey, string? optionsQuery,
            bool isActive, int displayOrder)
        {
            var entity = await _context.FilterDefinitions.FindAsync(filterDefinitionId);
            if (entity == null) return AdminOperationResult.Fail("Filtre tanimi bulunamadi.");

            var validation = await ValidateAsync(filterDefinitionId, filterKey, label, scope, dataSourceKey, optionsQuery);
            if (!validation.Success) return validation;

            var oldSnap = new { entity.FilterDefinitionId, entity.FilterKey, entity.Label, entity.Scope, entity.DataSourceKey, entity.IsActive, entity.DisplayOrder };

            entity.FilterKey = filterKey!.Trim();
            entity.Label = label!.Trim();
            entity.Scope = scope!.Trim();
            entity.DataSourceKey = string.IsNullOrWhiteSpace(dataSourceKey) ? null : dataSourceKey.Trim();
            entity.OptionsQuery = string.IsNullOrWhiteSpace(optionsQuery) ? null : optionsQuery.Trim();
            entity.IsActive = isActive;
            entity.DisplayOrder = displayOrder;
            entity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "filter_definition_update",
                TargetType = "filter_definition",
                TargetKey = entity.FilterDefinitionId.ToString(),
                Description = $"FilterDefinition '{entity.FilterKey}' updated",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                NewValuesJson = AuditLogService.ToJson(new { entity.FilterDefinitionId, entity.FilterKey, entity.Label, entity.Scope, entity.DataSourceKey, entity.IsActive, entity.DisplayOrder }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Filtre tanimi guncellendi.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int filterDefinitionId)
        {
            var entity = await _context.FilterDefinitions.FindAsync(filterDefinitionId);
            if (entity == null) return AdminOperationResult.Fail("Filtre tanimi bulunamadi.");

            var hasUsage = await _context.UserDataFilters
                .AnyAsync(udf => udf.FilterKey == entity.FilterKey);
            if (hasUsage)
            {
                return AdminOperationResult.Fail(
                    "Bu filtre kullanici atamalarinda kullaniliyor. Once IsActive=0 yapin veya atamalari temizleyin.");
            }

            var oldSnap = new { entity.FilterDefinitionId, entity.FilterKey, entity.Label, entity.Scope, entity.DataSourceKey };

            _context.FilterDefinitions.Remove(entity);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "filter_definition_delete",
                TargetType = "filter_definition",
                TargetKey = filterDefinitionId.ToString(),
                Description = $"FilterDefinition '{entity.FilterKey}' deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Filtre tanimi silindi.");
        }

        /// <summary>
        /// OptionsQuery icin SELECT-only kontrol. Faz 3'te FilterOptionsService exec etmeden once cagrilir.
        /// Admin yaziyor ama tek satir typo ile DB yorabilir — defansif.
        /// </summary>
        public static bool IsSafeOptionsQuery(string? optionsQuery, out string? reason)
        {
            reason = null;
            if (string.IsNullOrWhiteSpace(optionsQuery)) { reason = "OptionsQuery bos."; return false; }

            var trimmed = optionsQuery.Trim();
            if (!trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
            {
                reason = "Yalnizca SELECT (veya WITH ... SELECT) sorgusu kabul edilir.";
                return false;
            }

            if (DangerousSqlPattern.IsMatch(trimmed))
            {
                reason = "Sorgu tehlikeli anahtar kelime iceriyor (EXEC/INSERT/UPDATE/DELETE/DROP/MERGE/ALTER/TRUNCATE/GRANT/REVOKE/CREATE).";
                return false;
            }

            if (trimmed.Contains(';'))
            {
                reason = "Tek sorgu olmali, ';' ile ayrilmis multi-statement reddedilir.";
                return false;
            }

            return true;
        }

        private async Task<AdminOperationResult> ValidateAsync(
            int? existingId, string? filterKey, string? label, string? scope,
            string? dataSourceKey, string? optionsQuery)
        {
            var key = (filterKey ?? "").Trim();
            if (string.IsNullOrWhiteSpace(key))
                return AdminOperationResult.Fail("FilterKey zorunludur.");
            if (!FilterKeyPattern.IsMatch(key))
                return AdminOperationResult.Fail("FilterKey yalniz harf/rakam/_ icerebilir, harf ile baslamalidir.");

            if (string.IsNullOrWhiteSpace(label))
                return AdminOperationResult.Fail("Label zorunludur.");

            var scopeNorm = (scope ?? "").Trim();
            if (scopeNorm != FilterDefinition.ScopeSpInjection && scopeNorm != FilterDefinition.ScopeReportAccess)
                return AdminOperationResult.Fail("Scope yalniz 'spInjection' veya 'reportAccess' olabilir.");

            if (scopeNorm == FilterDefinition.ScopeSpInjection)
            {
                if (string.IsNullOrWhiteSpace(dataSourceKey))
                    return AdminOperationResult.Fail("spInjection scope icin DataSourceKey zorunludur.");
                if (string.IsNullOrWhiteSpace(optionsQuery))
                    return AdminOperationResult.Fail("spInjection scope icin OptionsQuery zorunludur.");
                if (!IsSafeOptionsQuery(optionsQuery, out var reason))
                    return AdminOperationResult.Fail($"OptionsQuery gecersiz: {reason}");

                var dsExists = await _context.DataSources.AsNoTracking()
                    .AnyAsync(d => d.DataSourceKey == dataSourceKey);
                if (!dsExists)
                    return AdminOperationResult.Fail("Belirtilen DataSourceKey bulunamadi.");
            }
            else // reportAccess
            {
                if (!string.IsNullOrWhiteSpace(dataSourceKey))
                    return AdminOperationResult.Fail("reportAccess scope DataSourceKey almamali (native EF source).");
                if (!string.IsNullOrWhiteSpace(optionsQuery))
                    return AdminOperationResult.Fail("reportAccess scope OptionsQuery almamali (native EF source).");
            }

            var duplicate = await _context.FilterDefinitions.AsNoTracking()
                .AnyAsync(f => f.FilterKey == key && (existingId == null || f.FilterDefinitionId != existingId));
            if (duplicate)
                return AdminOperationResult.Fail("Bu FilterKey zaten mevcut.");

            return AdminOperationResult.Ok("");
        }
    }
}
