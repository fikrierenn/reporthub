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
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public ReportsController(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        private string CurrentUserName => User.Identity?.Name ?? "user";

        private string[] CurrentUserRoles => User.Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Distinct()
            .ToArray();

        public async Task<IActionResult> Index(int? reportId)
        {
            var context = await BuildReportsContext(reportId);
            var model = new ReportsIndexViewModel
            {
                UserRoles = context.UserRoles,
                Reports = context.Reports
            };
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Run(int reportId)
        {
            var context = await BuildReportsContext(reportId);
            if (context.SelectedReport == null)
            {
                return RedirectToAction("Index");
            }

            var viewMode = ResolveViewMode(Request.Query["viewMode"].ToString());
            var model = new ReportRunViewModel
            {
                SelectedReport = context.SelectedReport,
                ParamFields = context.ParamFields,
                ViewMode = viewMode.ViewMode,
                BodyClass = viewMode.BodyClass
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(int reportId, IFormCollection form)
        {
            var context = await BuildReportsContext(reportId);
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

            var searchTerm = form["ResultSearch"].ToString();
            model.ResultSearch = searchTerm;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
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
            catch (Exception ex)
            {
                stopwatch.Stop();
                model.RunError = "Rapor calistirma hatasi: " + ex.Message;

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
            var context = await BuildReportsContext(reportId);
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

            var excelHtml = BuildExcelHtml(
                result.Rows,
                context.SelectedReport.Title ?? "",
                CurrentUserName,
                DateTime.Now,
                validation.ParamValues);
            var fileName = $"report_{context.SelectedReport.ReportId}_{DateTime.Now:yyyyMMdd_HHmmss}.xls";
            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(excelHtml))
                .ToArray();

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

            return File(bytes, "application/vnd.ms-excel; charset=utf-8", fileName);
        }

        private static bool AllowedForUser(string allowedRolesCsv, string userRolesCsv)
        {
            if (string.IsNullOrWhiteSpace(allowedRolesCsv))
            {
                return false;
            }

            var allowedRoles = allowedRolesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var userRoles = userRolesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return userRoles.Any(r => allowedRoles.Contains(r));
        }

        private async Task<ReportsContext> BuildReportsContext(int? reportId)
        {
            var allReportsQuery = _context.ReportCatalog
                .Include(r => r.DataSource)
                .Where(r => r.IsActive && r.DataSource != null && r.DataSource.IsActive);

            var allReports = await allReportsQuery
                .OrderBy(r => r.Title)
                .ToListAsync();

            var userRolesCsv = string.Join(",", CurrentUserRoles);

            var accessibleReports = allReports
                .Where(r => AllowedForUser(r.AllowedRoles, userRolesCsv))
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
                            DefaultValue = e.TryGetProperty("default", out var d) ? d.GetString() ?? "" : ""
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

            if (string.Equals(field.Type, "date", StringComparison.OrdinalIgnoreCase) &&
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

        private static string BuildExcelHtml(
            List<Dictionary<string, object>> rows,
            string reportTitle,
            string username,
            DateTime runAt,
            Dictionary<string, string> paramValues)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<html><head><meta charset=\"utf-8\"></head><body>");
            sb.AppendLine("<table border=\"1\">");
            sb.AppendLine("<tr><th colspan=\"4\">Rapor Ozeti</th></tr>");
            sb.AppendLine("<tr>");
            sb.Append("<td><strong>Rapor</strong></td><td>")
                .Append(System.Net.WebUtility.HtmlEncode(reportTitle))
                .AppendLine("</td>");
            sb.Append("<td><strong>Kullanici</strong></td><td>")
                .Append(System.Net.WebUtility.HtmlEncode(username))
                .AppendLine("</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("<tr>");
            sb.Append("<td><strong>Tarih</strong></td><td>")
                .Append(System.Net.WebUtility.HtmlEncode(runAt.ToString("yyyy-MM-dd HH:mm:ss")))
                .AppendLine("</td>");
            sb.Append("<td><strong>Parametreler</strong></td><td>");
            if (paramValues.Count == 0)
            {
                sb.Append("-");
            }
            else
            {
                var items = paramValues
                    .Select(kvp => $"{kvp.Key}={kvp.Value}")
                    .ToArray();
                sb.Append(System.Net.WebUtility.HtmlEncode(string.Join(", ", items)));
            }
            sb.AppendLine("</td>");
            sb.AppendLine("</tr>");
            sb.AppendLine("</table>");

            if (rows.Count > 0)
            {
                sb.AppendLine("<br/>");
                sb.AppendLine("<table border=\"1\">");
                var headers = rows[0].Keys.ToList();
                sb.AppendLine("<thead><tr>");
                foreach (var header in headers)
                {
                    sb.Append("<th>")
                        .Append(System.Net.WebUtility.HtmlEncode(header))
                        .AppendLine("</th>");
                }
                sb.AppendLine("</tr></thead>");

                sb.AppendLine("<tbody>");
                foreach (var row in rows)
                {
                    sb.AppendLine("<tr>");
                    foreach (var header in headers)
                    {
                        var value = row.TryGetValue(header, out var v) ? v?.ToString() ?? "" : "";
                        sb.Append("<td>")
                            .Append(System.Net.WebUtility.HtmlEncode(value))
                            .AppendLine("</td>");
                    }
                    sb.AppendLine("</tr>");
                }
                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }
            else
            {
                sb.AppendLine("<br/>");
                sb.AppendLine("<table border=\"1\">");
                sb.AppendLine("<tbody>");
                sb.AppendLine("<tr><td colspan=\"4\">Bos sonuc bulundu.</td></tr>");
                sb.AppendLine("</tbody>");
                sb.AppendLine("</table>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
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
