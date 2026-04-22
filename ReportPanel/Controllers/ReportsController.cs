using System.Data;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
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
        // G-03: Multi-tenant data filter whitelist'leri.
        // FilterKey SP parametre adina donustugu icin T-SQL identifier kurallarina uymali.
        // FilterValue STRING_SPLIT ile CSV olarak parse ediliyor — sadece alfanumerik + virgul + tire + alt tire + nokta + bosluk.
        private static readonly Regex FilterKeyRegex = new(@"^[a-zA-Z_][a-zA-Z0-9_]{0,62}$", RegexOptions.Compiled);
        private static readonly Regex FilterValueRegex = new(@"^[a-zA-Z0-9,_\-\. ]+$", RegexOptions.Compiled);

        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public ReportsController(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
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
                    CreatedAt = DateTime.Now
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
            var isDashboard = context.SelectedReport.ReportType == "dashboard";
            var model = new ReportRunViewModel
            {
                SelectedReport = context.SelectedReport,
                ParamFields = context.ParamFields,
                ViewMode = viewMode.ViewMode,
                BodyClass = viewMode.BodyClass,
                IsDashboard = isDashboard
            };

            // Dashboard + parametresiz → otomatik çalıştır
            if (isDashboard && !context.ParamFields.Any(f => f.Required))
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

            // Kullanıcı veri filtrelerini SP parametrelerine ekle
            await InjectUserDataFilters(
                validation.Parameters,
                context.SelectedReport.ReportId,
                context.SelectedReport.DataSourceKey);

            var searchTerm = form["ResultSearch"].ToString();
            model.ResultSearch = searchTerm;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var isDashboard = context.SelectedReport.ReportType == "dashboard";

            try
            {
                var hasConfig = isDashboard && !string.IsNullOrWhiteSpace(context.SelectedReport.DashboardConfigJson);
                var hasHtml = isDashboard && !string.IsNullOrWhiteSpace(context.SelectedReport.DashboardHtml);

                if (isDashboard && (hasConfig || hasHtml))
                {
                    var resultSets = await ExecuteStoredProcedureMultiResultSets(
                        context.SelectedReport.DataSource.ConnString,
                        context.SelectedReport.ProcName,
                        validation.Parameters);

                    stopwatch.Stop();

                    var totalRows = resultSets.Sum(rs => rs.Count);
                    model.RunSuccess = true;
                    model.IsDashboard = true;
                    model.RunMessage = $"Dashboard basariyla yuklendi. {resultSets.Count} result set, toplam {totalRows} kayit.";
                    model.RunRowCount = totalRows;
                    model.RunDurationMs = stopwatch.ElapsedMilliseconds;

                    if (hasConfig)
                    {
                        DashboardConfig? dashConfig = null;
                        try
                        {
                            dashConfig = JsonSerializer.Deserialize<DashboardConfig>(
                                context.SelectedReport.DashboardConfigJson!);
                        }
                        catch (JsonException jx)
                        {
                            // Bozuk config 500 patlatmasin. Bos config ile fallback render + uyari.
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
                        model.DashboardRenderedHtml = DashboardRenderer.Render(
                            dashConfig ?? new DashboardConfig(), resultSets);
                    }
                    else
                    {
                        model.DashboardRenderedHtml = RenderDashboardTemplate(
                            context.SelectedReport.DashboardHtml!, resultSets);
                    }

                    await LogRun(
                        context.SelectedReport,
                        validation.ParamsJson,
                        true,
                        totalRows,
                        (int)stopwatch.ElapsedMilliseconds,
                        null);
                }
                else
                {
                    var result = await ExecuteStoredProcedure(
                        context.SelectedReport.DataSource.ConnString,
                        context.SelectedReport.ProcName,
                        validation.Parameters);

                    stopwatch.Stop();

                    model.RunSuccess = true;
                    model.RunMessage = $"Rapor basariyla calistirildi. {result.Rows.Count} kayit donduruldu.";
                    model.RunRowCount = result.Rows.Count;
                    model.RunDurationMs = stopwatch.ElapsedMilliseconds;
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        var term = searchTerm.Trim().ToLowerInvariant();
                        result.Rows = result.Rows
                            .Where(row => row.Values.Any(value =>
                                (value?.ToString() ?? string.Empty).ToLowerInvariant().Contains(term)))
                            .ToList();
                    }

                    model.RunData = result.Rows;

                    await LogRun(
                        context.SelectedReport,
                        validation.ParamsJson,
                        true,
                        result.Rows.Count,
                        (int)stopwatch.ElapsedMilliseconds,
                        null);
                }
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

            var result = await ExecuteStoredProcedure(
                context.SelectedReport.DataSource.ConnString,
                context.SelectedReport.ProcName,
                validation.Parameters);

            var bytes = BuildExcelFile(
                result.Rows,
                context.SelectedReport.Title ?? "",
                CurrentUserName,
                DateTime.Now,
                validation.ParamValues);
            var fileName = $"report_{context.SelectedReport.ReportId}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

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

        private sealed class ReportRunResult
        {
            public List<Dictionary<string, object>> Rows { get; set; } = new();
        }

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

        private static async Task<ReportRunResult> ExecuteStoredProcedure(
            string connectionString,
            string procName,
            List<SqlParameter> parameters)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(procName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parameters.Count > 0)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }

            using var reader = await command.ExecuteReaderAsync();
            var result = new ReportRunResult();

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    row[reader.GetName(i)] = value ?? "";
                }

                result.Rows.Add(row);
            }

            return result;
        }

        private static async Task<List<List<Dictionary<string, object>>>> ExecuteStoredProcedureMultiResultSets(
            string connectionString,
            string procName,
            List<SqlParameter> parameters)
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(procName, connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 120
            };

            if (parameters.Count > 0)
            {
                command.Parameters.AddRange(parameters.ToArray());
            }

            using var reader = await command.ExecuteReaderAsync();
            var allResultSets = new List<List<Dictionary<string, object>>>();

            do
            {
                var rows = new List<Dictionary<string, object>>();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                        row[reader.GetName(i)] = value ?? "";
                    }
                    rows.Add(row);
                }
                allResultSets.Add(rows);
            } while (await reader.NextResultAsync());

            return allResultSets;
        }

        private static string RenderDashboardTemplate(
            string template,
            List<List<Dictionary<string, object>>> resultSets)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = null,
                WriteIndented = false
            };

            // Build JS variable declarations for each result set
            var dataScript = new System.Text.StringBuilder();
            dataScript.AppendLine("<script>");
            for (var i = 0; i < resultSets.Count; i++)
            {
                var json = JsonSerializer.Serialize(resultSets[i], options);
                // Escape </script> in JSON data to prevent premature tag closure
                json = json.Replace("</script>", "<\\/script>");
                dataScript.AppendLine($"const RESULT_{i} = {json};");
            }
            dataScript.AppendLine("</script>");

            // Replace placeholders like {{RESULT_0}} with variable references
            var html = template;
            for (var i = 0; i < resultSets.Count; i++)
            {
                html = html.Replace($"{{{{RESULT_{i}}}}}", $"RESULT_{i}");
            }

            // Replace any remaining unreferenced placeholders with empty arrays
            html = Regex.Replace(html, @"\{\{RESULT_\d+\}\}", "[]");

            // Inject Chart.js CDN if not already present
            if (!html.Contains("chart.js", StringComparison.OrdinalIgnoreCase) &&
                !html.Contains("Chart.min.js", StringComparison.OrdinalIgnoreCase))
            {
                var chartCdn = "<script src=\"https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js\"></script>";
                if (html.Contains("</head>", StringComparison.OrdinalIgnoreCase))
                {
                    html = html.Replace("</head>", chartCdn + "\n</head>");
                }
                else
                {
                    html = chartCdn + "\n" + html;
                }
            }

            // Inject data script right after <body> tag, or at the beginning
            if (html.Contains("<body", StringComparison.OrdinalIgnoreCase))
            {
                var bodyCloseIdx = html.IndexOf('>', html.IndexOf("<body", StringComparison.OrdinalIgnoreCase)) + 1;
                html = html.Insert(bodyCloseIdx, "\n" + dataScript.ToString());
            }
            else
            {
                html = dataScript.ToString() + html;
            }

            return html;
        }

        /// <summary>
        /// Kullanıcının UserDataFilter kayıtlarını okur ve SP parametrelerine ekler.
        /// FilterKey → @FilterKey_Filtre parametresi olarak enjekte edilir.
        /// Örnek: sube → @sube_Filtre = 'FSM,HEYKEL' veya NULL (filtre yoksa)
        /// SP tarafında: WHERE (@sube_Filtre IS NULL OR Sube IN (SELECT value FROM STRING_SPLIT(@sube_Filtre, ',')))
        /// </summary>
        private async Task InjectUserDataFilters(
            List<SqlParameter> parameters,
            int reportId,
            string dataSourceKey)
        {
            var userId = CurrentUserId;
            if (userId == null) return;

            // Kullanıcının tüm filtrelerini çek (bu rapor + genel)
            var filters = await _context.UserDataFilters
                .Where(f => f.UserId == userId.Value
                    && (f.ReportId == null || f.ReportId == reportId)
                    && (f.DataSourceKey == null || f.DataSourceKey == dataSourceKey))
                .ToListAsync();

            if (!filters.Any()) return; // filtre yok = tümünü gör

            // G-03: Whitelist. FilterKey T-SQL identifier kurallarina uymali,
            // FilterValue STRING_SPLIT safe karakter setinde olmali.
            var validFilters = filters
                .Where(f => FilterKeyRegex.IsMatch(f.FilterKey) && FilterValueRegex.IsMatch(f.FilterValue))
                .ToList();

            // Reject edilenleri audit log'a yaz (multi-tenant ihlal sinyali olabilir)
            var rejected = filters.Except(validFilters).ToList();
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

            // FilterKey bazında grupla ve virgülle birleştir
            var grouped = validFilters
                .GroupBy(f => f.FilterKey.ToLowerInvariant())
                .ToDictionary(
                    g => g.Key,
                    g => string.Join(",", g.Select(f => f.FilterValue)));

            // Her filtre grubu için @key_Filtre parametresi ekle
            // Zaten aynı isimde parametre varsa (kullanıcı formdan girmiş) ekleme
            foreach (var kvp in grouped)
            {
                var paramName = $"@{kvp.Key}_Filtre";
                if (parameters.Any(p => p.ParameterName.Equals(paramName, StringComparison.OrdinalIgnoreCase)))
                    continue;

                parameters.Add(new SqlParameter(paramName, SqlDbType.NVarChar, 500)
                {
                    Value = kvp.Value
                });
            }
        }

        private static byte[] BuildExcelFile(
            List<Dictionary<string, object>> rows,
            string reportTitle,
            string username,
            DateTime runAt,
            Dictionary<string, string> paramValues)
        {
            using var workbook = new XLWorkbook();

            var summary = workbook.Worksheets.Add("Summary");
            summary.Cell(1, 1).Value = "Rapor Ozeti";
            summary.Range(1, 1, 1, 2).Merge().Style.Font.SetBold();

            summary.Cell(2, 1).Value = "Rapor";
            summary.Cell(2, 2).Value = reportTitle;
            summary.Cell(3, 1).Value = "Kullanici";
            summary.Cell(3, 2).Value = username;
            summary.Cell(4, 1).Value = "Tarih";
            summary.Cell(4, 2).Value = runAt.ToString("yyyy-MM-dd HH:mm:ss");
            summary.Cell(5, 1).Value = "Parametreler";
            summary.Cell(5, 2).Value = paramValues.Count == 0
                ? "-"
                : string.Join(", ", paramValues.Select(kvp => $"{kvp.Key}={kvp.Value}"));
            summary.Columns().AdjustToContents();

            var results = workbook.Worksheets.Add("Results");
            if (rows.Count > 0)
            {
                var headers = rows[0].Keys.ToList();
                for (var i = 0; i < headers.Count; i++)
                {
                    results.Cell(1, i + 1).Value = headers[i];
                    results.Cell(1, i + 1).Style.Font.SetBold();
                }

                for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
                {
                    var row = rows[rowIndex];
                    for (var colIndex = 0; colIndex < headers.Count; colIndex++)
                    {
                        var header = headers[colIndex];
                        var value = row.TryGetValue(header, out var v) ? v : "";
                        results.Cell(rowIndex + 2, colIndex + 1).Value = value?.ToString() ?? "";
                    }
                }
            }
            else
            {
                results.Cell(1, 1).Value = "Bos sonuc bulundu.";
            }

            results.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

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
