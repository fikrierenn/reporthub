using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        // G-03: Multi-tenant data filter whitelist'i UserDataFilterValidator'da (test edilebilir).

        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;
        private readonly ExcelExportService _excelExport;
        private readonly UserDataFilterInjector _filterInjector;
        private readonly StoredProcedureExecutor _spExecutor;

        public ReportsController(
            ReportPanelContext context,
            AuditLogService auditLog,
            ExcelExportService excelExport,
            UserDataFilterInjector filterInjector,
            StoredProcedureExecutor spExecutor)
        {
            _context = context;
            _auditLog = auditLog;
            _excelExport = excelExport;
            _filterInjector = filterInjector;
            _spExecutor = spExecutor;
        }

        private string CurrentUserName => User.Identity?.Name ?? "user";

        private int? CurrentUserId
        {
            get
            {
                var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
                return int.TryParse(value, out var userId) ? userId : null;
            }
        }

        private string[] CurrentUserRoles => User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        public async Task<IActionResult> Index(int? reportId, string? q, string? category)
        {
            var context = await BuildReportsContext(reportId, q, category);
            var model = new ReportsIndexViewModel
            {
                UserRoles = context.UserRoles,
                Reports = context.Reports,
                FavoriteReportIds = context.FavoriteReportIds,
                Categories = context.Categories,
                SearchTerm = context.SearchTerm,
                SelectedCategory = context.SelectedCategory
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Favorite(int reportId, string? returnUrl = null)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return RedirectToAction("Index");
            }

            var userRoleIds = await GetCurrentUserRoleIds();
            var report = await _context.ReportCatalog
                .AsNoTracking()
                .Include(r => r.ReportAllowedRoles)
                .FirstOrDefaultAsync(r => r.ReportId == reportId && r.IsActive);
            if (report == null || !report.ReportAllowedRoles.Any(ar => userRoleIds.Contains(ar.RoleId)))
            {
                return RedirectToAction("Index");
            }

            var exists = await _context.ReportFavorites
                .AnyAsync(f => f.UserId == userId.Value && f.ReportId == reportId);
            if (!exists)
            {
                _context.ReportFavorites.Add(new ReportFavorite
                {
                    UserId = userId.Value,
                    ReportId = reportId,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "report_favorite_add",
                    TargetType = "report",
                    TargetKey = reportId.ToString(),
                    ReportId = reportId,
                    Description = "Report favorited",
                    IsSuccess = true
                });
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unfavorite(int reportId, string? returnUrl = null)
        {
            var userId = CurrentUserId;
            if (userId == null)
            {
                return RedirectToAction("Index");
            }

            var favorite = await _context.ReportFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId.Value && f.ReportId == reportId);
            if (favorite != null)
            {
                _context.ReportFavorites.Remove(favorite);
                await _context.SaveChangesAsync();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "report_favorite_remove",
                    TargetType = "report",
                    TargetKey = reportId.ToString(),
                    ReportId = reportId,
                    Description = "Report unfavorited",
                    IsSuccess = true
                });
            }

            return RedirectToLocal(returnUrl);
        }

        [HttpGet]
        public async Task<IActionResult> Run(int reportId)
        {
            var context = await BuildReportsContext(reportId, null, null);
            if (context.SelectedReport == null)
            {
                return RedirectToAction("Index");
            }

            var viewMode = ResolveViewMode(Request.Query["viewMode"].ToString());
            // ADR-009: Tüm raporlar dashboard. IsDashboard ViewModel'da kaldı (alt-commit 2 Razor rewrite sonrası sil).
            var model = new ReportRunViewModel
            {
                SelectedReport = context.SelectedReport,
                ParamFields = context.ParamFields,
                ViewMode = viewMode.ViewMode,
                BodyClass = viewMode.BodyClass,
                IsDashboard = true
            };

            // Parametresiz → otomatik çalıştır (hepsi dashboard)
            if (!context.ParamFields.Any(f => f.Required))
            {
                var fakeForm = new Microsoft.AspNetCore.Http.FormCollection(
                    context.ParamFields.ToDictionary(
                        f => f.Name,
                        f => new Microsoft.Extensions.Primitives.StringValues(
                            string.Equals(f.Type, "date", StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(f.DefaultValue, "today", StringComparison.OrdinalIgnoreCase)
                                ? DateTime.Today.ToString("yyyy-MM-dd")
                                : f.DefaultValue ?? "")));
                return await Run(context.SelectedReport.ReportId, fakeForm);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(int reportId, IFormCollection form)
        {
            var context = await BuildReportsContext(reportId, null, null);
            var viewMode = ResolveViewMode(form["viewMode"].ToString());
            var model = new ReportRunViewModel
            {
                SelectedReport = context.SelectedReport,
                ParamFields = context.ParamFields,
                ViewMode = viewMode.ViewMode,
                BodyClass = viewMode.BodyClass
            };

            if (context.SelectedReport == null)
            {
                model.RunError = "Report not found or access denied.";
                return View("Run", model);
            }

            var validation = ValidateAndBuildParameters(context.ParamFields, form);
            model.ParamValues = validation.ParamValues;
            if (!validation.Success)
            {
                model.RunError = string.Join(" ", validation.Errors);
                return View("Run", model);
            }

            if (context.SelectedReport.DataSource == null || !context.SelectedReport.DataSource.IsActive)
            {
                model.RunError = "Data source not found or inactive.";
                return View("Run", model);
            }

            // Kullanıcı veri filtrelerini SP parametrelerine ekle (M-13 R6.2: UserDataFilterInjector).
            await _filterInjector.InjectAsync(
                validation.Parameters,
                CurrentUserId,
                context.SelectedReport.ReportId,
                context.SelectedReport.DataSourceKey);

            var searchTerm = form["ResultSearch"].ToString();
            model.ResultSearch = searchTerm;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            model.IsDashboard = true; // ADR-009: tüm raporlar dashboard.

            try
            {
                // ADR-009: Tek path. Eski else-branch (tablo render) DROP edildi.
                // Migration 18 sonrası her raporda DashboardConfigJson dolu. Boş kalan edge case'e
                // karşı dashboard_config_missing audit + boş şablon fallback korunuyor.
                var hasConfig = !string.IsNullOrWhiteSpace(context.SelectedReport.DashboardConfigJson);

                var resultSets = await _spExecutor.ExecuteMultipleAsync(
                    context.SelectedReport.DataSource.ConnString,
                    context.SelectedReport.ProcName,
                    validation.Parameters);

                stopwatch.Stop();

                var totalRows = resultSets.Sum(rs => rs.Count);
                model.RunSuccess = true;
                model.RunMessage = $"Dashboard basariyla yuklendi. {resultSets.Count} result set, toplam {totalRows} kayit.";
                model.RunRowCount = totalRows;
                model.RunDurationMs = stopwatch.ElapsedMilliseconds;

                DashboardConfig? dashConfig = null;
                if (hasConfig)
                {
                    try
                    {
                        dashConfig = JsonSerializer.Deserialize<DashboardConfig>(
                            context.SelectedReport.DashboardConfigJson!);
                    }
                    catch (JsonException jx)
                    {
                        await _auditLog.LogAsync(new AuditLogEntry
                        {
                            EventType = "dashboard_config_invalid",
                            TargetType = "report",
                            TargetKey = context.SelectedReport.ReportId.ToString(),
                            ReportId = context.SelectedReport.ReportId,
                            Description = $"DashboardConfigJson deserialize failed: {jx.Message}",
                            IsSuccess = false
                        });
                        model.RunMessage = (model.RunMessage ?? "") +
                            " (UYARI: Dashboard yapilandirmasi bozuk, bos sablonla gosteriliyor. Admin'e bildirin.)";
                    }
                }
                else
                {
                    await _auditLog.LogAsync(new AuditLogEntry
                    {
                        EventType = "dashboard_config_missing",
                        TargetType = "report",
                        TargetKey = context.SelectedReport.ReportId.ToString(),
                        ReportId = context.SelectedReport.ReportId,
                        Description = "Dashboard report has no DashboardConfigJson. Bos sablon render edildi.",
                        IsSuccess = false
                    });
                    model.RunMessage = (model.RunMessage ?? "") +
                        " (UYARI: Dashboard yapilandirmasi yok, bos sablonla gosteriliyor. Admin'e bildirin.)";
                }
                model.DashboardRenderedHtml = DashboardRenderer.Render(
                    dashConfig ?? new DashboardConfig(), resultSets);

                await LogRun(
                    context.SelectedReport,
                    validation.ParamsJson,
                    true,
                    totalRows,
                    (int)stopwatch.ElapsedMilliseconds,
                    null);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // M-02: user'a generic mesaj, detay audit log'a gider.
                model.RunError = "Rapor çalıştırılırken hata oluştu. Parametreleri kontrol edin veya sistem yöneticisine başvurun.";

                await LogRun(
                    context.SelectedReport,
                    validation.ParamsJson,
                    false,
                    0,
                    (int)stopwatch.ElapsedMilliseconds,
                    ex.Message);
            }

            return View("Run", model);
        }

        private static (string ViewMode, string BodyClass) ResolveViewMode(string? viewMode)
        {
            var mode = viewMode?.Trim().ToLowerInvariant() ?? "";
            var bodyClass = mode switch
            {
                "wide" => "wide-view",
                "table" => "table-focus",
                "full" => "full-screen",
                _ => ""
            };
            return (mode, bodyClass);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(int reportId)
        {
            var context = await BuildReportsContext(reportId, null, null);
            if (context.SelectedReport == null)
            {
                return BadRequest("Report not found or access denied.");
            }

            var paramFields = ParseParamSchema(context.SelectedReport.ParamSchemaJson);
            var validation = ValidateAndBuildParameters(paramFields, Request.Form);
            if (!validation.Success)
            {
                return BadRequest(string.Join(" ", validation.Errors));
            }

            if (context.SelectedReport.DataSource == null || !context.SelectedReport.DataSource.IsActive)
            {
                return BadRequest("Data source not found or inactive.");
            }

            var result = await _spExecutor.ExecuteAsync(
                context.SelectedReport.DataSource.ConnString,
                context.SelectedReport.ProcName,
                validation.Parameters);

            var bytes = _excelExport.BuildReportXlsx(
                result.Rows,
                context.SelectedReport.Title ?? "",
                CurrentUserName,
                DateTime.UtcNow,
                validation.ParamValues);
            var fileName = $"report_{context.SelectedReport.ReportId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "report_export",
                TargetType = "report",
                TargetKey = context.SelectedReport.ReportId.ToString(),
                ReportId = context.SelectedReport.ReportId,
                DataSourceKey = context.SelectedReport.DataSourceKey,
                ParamsJson = validation.ParamsJson,
                ResultRowCount = result.Rows.Count,
                IsSuccess = true,
                Description = $"Export {result.Rows.Count} rows"
            });

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        private async Task<List<int>> GetCurrentUserRoleIds()
        {
            if (!CurrentUserId.HasValue)
            {
                return new List<int>();
            }

            // M-03: Rol kaynağı artık yalnızca UserRole junction. CSV fallback kaldırıldı.
            return await _context.UserRoles
                .Where(ur => ur.UserId == CurrentUserId.Value)
                .Select(ur => ur.RoleId)
                .ToListAsync();
        }

        private async Task<ReportsContext> BuildReportsContext(int? reportId, string? searchTerm, string? category)
        {
            var userRoleIds = await GetCurrentUserRoleIds();

            var accessibleReportsQuery = _context.ReportCatalog
                .Include(r => r.DataSource)
                .Include(r => r.ReportAllowedRoles)
                    .ThenInclude(ar => ar.Role)
                .Include(r => r.ReportCategories)
                    .ThenInclude(rc => rc.Category)
                .Where(r => r.IsActive && r.DataSource != null && r.DataSource.IsActive);

            if (userRoleIds.Count > 0)
            {
                accessibleReportsQuery = accessibleReportsQuery
                    .Where(r => r.ReportAllowedRoles.Any(ar => userRoleIds.Contains(ar.RoleId)));
            }
            else
            {
                accessibleReportsQuery = accessibleReportsQuery.Where(r => false);
            }

            var accessibleReports = await accessibleReportsQuery
                .OrderBy(r => r.Title)
                .ToListAsync();

            var categories = accessibleReports
                .SelectMany(r => r.ReportCategories.Select(rc => rc.Category?.Name ?? ""))
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();

            var normalizedSearch = (searchTerm ?? "").Trim();
            var normalizedCategory = (category ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var term = normalizedSearch.ToLowerInvariant();
                accessibleReports = accessibleReports
                    .Where(r =>
                        (r.Title ?? "").ToLowerInvariant().Contains(term) ||
                        (r.Description ?? "").ToLowerInvariant().Contains(term) ||
                        r.ReportCategories.Any(rc => (rc.Category?.Name ?? "").ToLowerInvariant().Contains(term)) ||
                        (r.DataSource?.Title ?? "").ToLowerInvariant().Contains(term) ||
                        (r.DataSourceKey ?? "").ToLowerInvariant().Contains(term))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(normalizedCategory))
            {
                accessibleReports = accessibleReports
                    .Where(r => r.ReportCategories.Any(rc =>
                        string.Equals(rc.Category?.Name?.Trim(), normalizedCategory, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            var favoriteIds = new HashSet<int>();
            if (CurrentUserId.HasValue)
            {
                var ids = await _context.ReportFavorites
                    .Where(f => f.UserId == CurrentUserId.Value)
                    .Select(f => f.ReportId)
                    .ToListAsync();
                favoriteIds = ids.ToHashSet();
            }

            accessibleReports = accessibleReports
                .OrderByDescending(r => favoriteIds.Contains(r.ReportId))
                .ThenBy(r => r.ReportCategories.FirstOrDefault()?.Category?.Name ?? "")
                .ThenBy(r => r.Title)
                .ToList();

            ReportCatalog? selectedReport = null;
            if (reportId.HasValue)
            {
                selectedReport = accessibleReports.FirstOrDefault(r => r.ReportId == reportId.Value);
            }

            return new ReportsContext
            {
                UserName = CurrentUserName,
                UserRoles = CurrentUserRoles,
                Reports = accessibleReports,
                FavoriteReportIds = favoriteIds,
                Categories = categories,
                SearchTerm = normalizedSearch,
                SelectedCategory = normalizedCategory,
                SelectedReport = selectedReport,
                ParamFields = selectedReport != null
                    ? ParseParamSchema(selectedReport.ParamSchemaJson)
                    : new List<ReportParamField>()
            };
        }

        private sealed class ReportsContext
        {
            public string UserName { get; set; } = "";
            public string[] UserRoles { get; set; } = Array.Empty<string>();
            public List<ReportCatalog> Reports { get; set; } = new();
            public HashSet<int> FavoriteReportIds { get; set; } = new();
            public List<string> Categories { get; set; } = new();
            public string SearchTerm { get; set; } = "";
            public string SelectedCategory { get; set; } = "";
            public ReportCatalog? SelectedReport { get; set; }
            public List<ReportParamField> ParamFields { get; set; } = new();
        }

        private static List<ReportParamField> ParseParamSchema(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<ReportParamField>();
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("fields", out var fieldsElement) &&
                    fieldsElement.ValueKind == JsonValueKind.Array)
                {
                    return fieldsElement
                        .EnumerateArray()
                        .Where(e => e.ValueKind == JsonValueKind.Object)
                        .Select(e => new ReportParamField
                        {
                            Name = e.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                            Label = e.TryGetProperty("label", out var l) ? l.GetString() ?? "" : "",
                            Type = e.TryGetProperty("type", out var t) ? t.GetString() ?? "text" : "text",
                            Required = e.TryGetProperty("required", out var r) && r.GetBoolean(),
                            Placeholder = e.TryGetProperty("placeholder", out var p) ? p.GetString() ?? "" : "",
                            HelpText = e.TryGetProperty("help", out var h) ? h.GetString() ?? "" : "",
                            DefaultValue = e.TryGetProperty("default", out var d)
                                ? d.GetString() ?? ""
                                : (e.TryGetProperty("defaultValue", out var dv) ? dv.GetString() ?? "" : "")
                        })
                        .Where(f => !string.IsNullOrWhiteSpace(f.Name))
                        .ToList();
                }

                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var list = new List<ReportParamField>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        var type = prop.Value.ValueKind == JsonValueKind.String
                            ? prop.Value.GetString() ?? "text"
                            : "text";
                        list.Add(new ReportParamField
                        {
                            Name = prop.Name,
                            Label = prop.Name,
                            Type = NormalizeType(type),
                            Required = false,
                            Placeholder = "",
                            HelpText = "",
                            DefaultValue = ""
                        });
                    }

                    return list;
                }
            }
            catch
            {
                return new List<ReportParamField>();
            }

            return new List<ReportParamField>();
        }

        private static string NormalizeType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
            {
                return "text";
            }

            var lower = type.Trim().ToLowerInvariant();
            return lower switch
            {
                "int" => "number",
                "integer" => "number",
                "decimal" => "decimal",
                "float" => "decimal",
                "double" => "decimal",
                "bit" => "checkbox",
                "bool" => "checkbox",
                "boolean" => "checkbox",
                "date" => "date",
                "datetime" => "date",
                _ => lower
            };
        }

        private sealed class ParamValidationResult
        {
            public bool Success { get; set; }
            public List<string> Errors { get; set; } = new();
            public List<SqlParameter> Parameters { get; set; } = new();
            public string ParamsJson { get; set; } = "{}";
            public Dictionary<string, string> ParamValues { get; set; } = new();
        }

        private static ParamValidationResult ValidateAndBuildParameters(
            List<ReportParamField> fields,
            IFormCollection form)
        {
            var result = new ParamValidationResult { Success = true };
            var paramValues = new Dictionary<string, object?>();
            var rawValues = new Dictionary<string, string>();

            foreach (var field in fields)
            {
                var raw = form[field.Name].ToString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    raw = ResolveDefaultValue(field);
                }
                var isEmpty = string.IsNullOrWhiteSpace(raw);
                rawValues[field.Name] = raw;

                if (field.Required && isEmpty)
                {
                    result.Success = false;
                    result.Errors.Add($"{field.Label} is required.");
                    continue;
                }

                object? value = null;
                var type = NormalizeType(field.Type);

                if (!isEmpty)
                {
                    switch (type)
                    {
                        case "number":
                            if (int.TryParse(raw, out var intValue))
                            {
                                value = intValue;
                            }
                            else
                            {
                                result.Success = false;
                                result.Errors.Add($"{field.Label} must be a number.");
                            }
                            break;
                        case "decimal":
                            if (decimal.TryParse(raw, out var decimalValue))
                            {
                                value = decimalValue;
                            }
                            else
                            {
                                result.Success = false;
                                result.Errors.Add($"{field.Label} must be a number.");
                            }
                            break;
                        case "checkbox":
                            value = true;
                            break;
                        case "date":
                            if (DateTime.TryParse(raw, out var dateValue))
                            {
                                value = dateValue;
                            }
                            else
                            {
                                result.Success = false;
                                result.Errors.Add($"{field.Label} must be a valid date.");
                            }
                            break;
                        default:
                            value = raw.Trim();
                            break;
                    }
                }

                paramValues[field.Name] = value;
            }

            if (!result.Success)
            {
                return result;
            }

            foreach (var kvp in paramValues)
            {
                var param = new SqlParameter("@" + kvp.Key, kvp.Value ?? DBNull.Value);
                result.Parameters.Add(param);
            }

            result.ParamsJson = JsonSerializer.Serialize(paramValues);
            result.ParamValues = rawValues;
            return result;
        }

        // M-13 R6.3 (28 Nisan 2026): ReportRunResult → SpExecutionResult (StoredProcedureExecutor servisi).

        private static string ResolveDefaultValue(ReportParamField field)
        {
            if (string.IsNullOrWhiteSpace(field.DefaultValue))
            {
                return string.Empty;
            }

            if (string.Equals(NormalizeType(field.Type), "date", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(field.DefaultValue.Trim(), "today", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Today.ToString("yyyy-MM-dd");
            }

            return field.DefaultValue.Trim();
        }

        // M-13 R6.3 (28 Nisan 2026): ExecuteStoredProcedure + ExecuteStoredProcedureMultiResultSets → StoredProcedureExecutor.
        // M-13 R6.2 (28 Nisan 2026): InjectUserDataFilters → UserDataFilterInjector servisine.
        // M-13 R6.1 (28 Nisan 2026): BuildExcelFile static helper'i ExcelExportService'e tasindi.

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl)
                && Url.IsLocalUrl(returnUrl)
                && returnUrl.StartsWith("/", StringComparison.Ordinal)
                && !returnUrl.StartsWith("//", StringComparison.Ordinal)
                && !returnUrl.StartsWith("/\\", StringComparison.Ordinal))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Reports");
        }

        private async Task LogRun(
            ReportCatalog report,
            string paramsJson,
            bool isSuccess,
            int rowCount,
            int durationMs,
            string? errorMessage)
        {
            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "report_run",
                TargetType = "report",
                TargetKey = report.ReportId.ToString(),
                ReportId = report.ReportId,
                DataSourceKey = report.DataSourceKey,
                ParamsJson = paramsJson,
                DurationMs = durationMs,
                ResultRowCount = rowCount,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Description = isSuccess ? $"Run OK ({rowCount} rows)" : "Run failed"
            });
        }
    }
}
