using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    // M-05: DashboardHtml parametresi kaldirildi (legacy retirement). DashboardConfigJson
    // birincil. Mevcut DB'deki HTML kayitlari ReportsController legacy fallback ile render
    // edilir ama yeni yazim yollari dokunmaz.
    public record ReportFormInput(
        string? Title,
        string? Description,
        string? DataSourceKey,
        string? ProcName,
        HashSet<int> SelectedRoleIds,
        HashSet<int> SelectedCategoryIds,
        bool IsActive,
        string? ReportType,
        string? ParamSchemaJson,
        string? DashboardConfigJson);

    /// <summary>
    /// M-01: Report CRUD. HandlePostAction + dedicated CreateReport/EditReport
    /// action'lari ayni yola koyuyor: ReportFormInput DTO -> service.
    /// </summary>
    public class ReportManagementService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public ReportManagementService(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<AdminOperationResult> CreateAsync(ReportFormInput input)
        {
            var err = Validate(input);
            if (err != null) return AdminOperationResult.Fail(err);

            var reportType = NormalizeReportType(input.ReportType);
            if (reportType == "dashboard")
            {
                var dashErr = await ValidateDashboardConfigAsync(input.DashboardConfigJson, reportId: null, input);
                if (dashErr != null) return AdminOperationResult.Fail(dashErr);
            }

            var entity = new ReportCatalog
            {
                Title = (input.Title ?? "").Trim(),
                Description = input.Description ?? "",
                DataSourceKey = (input.DataSourceKey ?? "").Trim(),
                ProcName = (input.ProcName ?? "").Trim(),
                AllowedRoles = await BuildAllowedRolesCsv(input.SelectedRoleIds),
                IsActive = input.IsActive,
                ReportType = reportType,
                ParamSchemaJson = NormalizeParamSchema(input.ParamSchemaJson, null)
            };
            if (entity.ReportType == "dashboard")
            {
                entity.DashboardConfigJson = input.DashboardConfigJson;
            }

            _context.ReportCatalog.Add(entity);
            await _context.SaveChangesAsync();
            await SyncRolesAndCategoriesAsync(entity.ReportId, input.SelectedRoleIds, input.SelectedCategoryIds);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "report_create",
                TargetType = "report",
                TargetKey = entity.ReportId.ToString(),
                ReportId = entity.ReportId,
                DataSourceKey = entity.DataSourceKey,
                Description = "Report created",
                NewValuesJson = AuditLogService.ToJson(new { entity.ReportId, entity.Title, entity.DataSourceKey, entity.ProcName, entity.AllowedRoles, entity.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Rapor eklendi.");
        }

        public async Task<AdminOperationResult> UpdateAsync(int reportId, ReportFormInput input)
        {
            var report = await _context.ReportCatalog.FindAsync(reportId);
            if (report == null) return AdminOperationResult.Fail("Rapor bulunamadi.");

            var err = Validate(input);
            if (err != null) return AdminOperationResult.Fail(err);

            var reportType = NormalizeReportType(input.ReportType);
            if (reportType == "dashboard")
            {
                var dashErr = await ValidateDashboardConfigAsync(input.DashboardConfigJson, reportId, input);
                if (dashErr != null) return AdminOperationResult.Fail(dashErr);
            }

            var oldSnap = new { report.ReportId, report.Title, report.DataSourceKey, report.ProcName, report.AllowedRoles, report.IsActive };

            report.Title = (input.Title ?? "").Trim();
            report.Description = input.Description ?? "";
            report.DataSourceKey = (input.DataSourceKey ?? "").Trim();
            report.ProcName = (input.ProcName ?? "").Trim();
            report.AllowedRoles = await BuildAllowedRolesCsv(input.SelectedRoleIds);
            report.IsActive = input.IsActive;
            report.ParamSchemaJson = NormalizeParamSchema(input.ParamSchemaJson, report.ParamSchemaJson);
            report.ReportType = reportType;
            // M-05: DashboardHtml'e yeni yazim yok. ReportType dashboard degilse ConfigJson
            // null'a cekilir; legacy DashboardHtml DB'de oldugu gibi kalir (migration 16
            // orphan check ile tespit edilir, Faz C'de drop).
            report.DashboardConfigJson = report.ReportType == "dashboard" ? input.DashboardConfigJson : null;

            await _context.SaveChangesAsync();
            await SyncRolesAndCategoriesAsync(report.ReportId, input.SelectedRoleIds, input.SelectedCategoryIds);

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "report_update",
                TargetType = "report",
                TargetKey = report.ReportId.ToString(),
                ReportId = report.ReportId,
                DataSourceKey = report.DataSourceKey,
                Description = "Report updated",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                NewValuesJson = AuditLogService.ToJson(new { report.ReportId, report.Title, report.DataSourceKey, report.ProcName, report.AllowedRoles, report.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Rapor guncellendi.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int reportId)
        {
            var report = await _context.ReportCatalog.FindAsync(reportId);
            if (report == null) return AdminOperationResult.Fail("Rapor bulunamadi.");

            var oldSnap = new { report.ReportId, report.Title, report.DataSourceKey, report.ProcName, report.AllowedRoles, report.IsActive };
            _context.ReportCatalog.Remove(report);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "report_delete",
                TargetType = "report",
                TargetKey = report.ReportId.ToString(),
                ReportId = report.ReportId,
                DataSourceKey = report.DataSourceKey,
                Description = "Report deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Rapor silindi.");
        }

        private static string? Validate(ReportFormInput input)
        {
            if (string.IsNullOrWhiteSpace(input.Title)) return "Baslik zorunludur.";
            if (string.IsNullOrWhiteSpace(input.DataSourceKey)) return "Veri kaynagi secilmeli.";
            if (string.IsNullOrWhiteSpace(input.ProcName)) return "Prosedur adi zorunludur.";
            return null;
        }

        // M-10 Faz 3: Hard error save'i bloke eder + dashboard_config_invalid audit.
        // Soft warning save'e izin verir ama dashboard_config_warnings audit'e dusurulur.
        private async Task<string?> ValidateDashboardConfigAsync(string? configJson, int? reportId, ReportFormInput input)
        {
            var r = DashboardConfigValidator.Validate(configJson);
            var targetKey = reportId?.ToString() ?? "(yeni)";

            if (r.HasErrors)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "dashboard_config_invalid",
                    TargetType = "report",
                    TargetKey = targetKey,
                    ReportId = reportId,
                    DataSourceKey = input.DataSourceKey,
                    Description = "Pano yapılandırması kaydedilemedi (validasyon hatası).",
                    NewValuesJson = AuditLogService.ToJson(new { errors = r.Errors, warnings = r.Warnings }),
                    IsSuccess = false
                });
                return r.Errors[0];
            }

            if (r.HasWarnings)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "dashboard_config_warnings",
                    TargetType = "report",
                    TargetKey = targetKey,
                    ReportId = reportId,
                    DataSourceKey = input.DataSourceKey,
                    Description = "Pano yapılandırması uyarılarla kaydedildi.",
                    NewValuesJson = AuditLogService.ToJson(new { warnings = r.Warnings }),
                    IsSuccess = true
                });
            }

            return null;
        }

        private async Task<string> BuildAllowedRolesCsv(HashSet<int> roleIds)
        {
            if (roleIds.Count == 0) return "";
            var names = await _context.Roles
                .Where(r => roleIds.Contains(r.RoleId))
                .Select(r => r.Name)
                .ToListAsync();
            return string.Join(",", names);
        }

        private async Task SyncRolesAndCategoriesAsync(int reportId, HashSet<int> roleIds, HashSet<int> categoryIds)
        {
            var existingRoles = await _context.ReportAllowedRoles.Where(ar => ar.ReportId == reportId).ToListAsync();
            _context.ReportAllowedRoles.RemoveRange(existingRoles);
            foreach (var rid in roleIds)
                _context.ReportAllowedRoles.Add(new ReportAllowedRole { ReportId = reportId, RoleId = rid, CreatedAt = DateTime.UtcNow });

            var existingCategories = await _context.ReportCategoryLinks.Where(rc => rc.ReportId == reportId).ToListAsync();
            _context.ReportCategoryLinks.RemoveRange(existingCategories);
            foreach (var cid in categoryIds)
                _context.ReportCategoryLinks.Add(new ReportCategoryLink { ReportId = reportId, CategoryId = cid, CreatedAt = DateTime.UtcNow });

            await _context.SaveChangesAsync();
        }

        private static string NormalizeReportType(string? raw) =>
            raw == "dashboard" ? "dashboard" : "table";

        private static string NormalizeParamSchema(string? raw, string? fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.IsNullOrWhiteSpace(fallback) ? "{}" : fallback;
            return raw.Trim();
        }
    }
}
