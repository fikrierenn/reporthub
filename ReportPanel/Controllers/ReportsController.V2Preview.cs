using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit). M-11 V2 builder runtime path:
    // RunJsonV2 (V1 Run paritesi JSON), RunJsonV2Preview (CreateReportV2 reportId-less),
    // PreviewDashboardV2 (F-9 tam dashboard preview iframe).
    public partial class ReportsController
    {
        // V2 builder için — V1 Run POST'un birebir SP path'i, JSON döner. ParamSchema'dan
        // default'lar üretilir (date='today' → bugün), opsiyonel paramsJson override eder.
        // Diğer SP parametreleri (ParamSchema'da yok) NULL kalır → SP kendi default'ını kullanır.
        [HttpGet]
        [Route("Reports/RunJsonV2/{reportId:int}")]
        public async Task<IActionResult> RunJsonV2(int reportId, [FromQuery] string? paramsJson = null)
        {
            var context = await BuildReportsContext(reportId, null, null);
            if (context.SelectedReport == null)
                return Json(new { success = false, error = "Rapor bulunamadı veya yetki yok." });
            if (context.SelectedReport.DataSource == null || !context.SelectedReport.DataSource.IsActive)
                return Json(new { success = false, error = "Veri kaynağı pasif veya bulunamadı." });

            // FormCollection oluştur — paramsJson varsa kullan, kalan field'lar default
            var formDict = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            if (!string.IsNullOrWhiteSpace(paramsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson);
                    if (parsed != null)
                        foreach (var kv in parsed)
                            formDict[kv.Key] = new Microsoft.Extensions.Primitives.StringValues(kv.Value ?? "");
                }
                catch { /* sessizce default'lara düş */ }
            }
            foreach (var f in context.ParamFields)
            {
                if (formDict.ContainsKey(f.Name)) continue;
                var dv = string.Equals(f.Type, "date", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(f.DefaultValue, "today", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : f.DefaultValue ?? "";
                formDict[f.Name] = new Microsoft.Extensions.Primitives.StringValues(dv);
            }
            var fakeForm = new Microsoft.AspNetCore.Http.FormCollection(formDict);

            var validation = ReportParamValidator.ValidateAndBuild(context.ParamFields, fakeForm);
            if (!validation.Success)
                return Json(new { success = false, error = string.Join(" ", validation.Errors) });

            await _filterInjector.InjectAsync(
                validation.Parameters,
                CurrentUserId,
                context.SelectedReport.ReportId,
                context.SelectedReport.DataSourceKey);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var resultSets = await _spExecutor.ExecuteMultipleAsync(
                    context.SelectedReport.DataSource.ConnString,
                    context.SelectedReport.ProcName,
                    validation.Parameters);
                stopwatch.Stop();

                var totalRows = resultSets.Sum(rs => rs.Count);
                var serialized = resultSets.Select((rs, i) =>
                {
                    var first = rs.FirstOrDefault();
                    var cols = first != null ? first.Keys.ToList() : new List<string>();
                    return new
                    {
                        index = i,
                        name = GuessResultSetTitle(rs, cols, i),
                        columns = cols,
                        rows = rs.Take(50).ToList(),
                        rowCount = rs.Count
                    };
                }).ToList();

                await LogRun(
                    context.SelectedReport,
                    validation.ParamsJson,
                    true,
                    totalRows,
                    (int)stopwatch.ElapsedMilliseconds,
                    null);

                return Json(new { success = true, error = (string?)null, resultSets = serialized });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await LogRun(
                    context.SelectedReport,
                    validation.ParamsJson,
                    false,
                    0,
                    (int)stopwatch.ElapsedMilliseconds,
                    ex.Message);
                // M-02: user'a generic mesaj, detay audit log'a gider.
                return Json(new
                {
                    success = false,
                    error = "Rapor çalıştırılamadı. Parametreleri kontrol edin veya sistem yöneticisine başvurun.",
                    resultSets = Array.Empty<object>()
                });
            }
        }

        // V2 builder CreateReportV2 (rapor henüz DB'de yok) için reportId-less varyant.
        // RunJsonV2 ile aynı path: ParamSchema-only param geçişi, SP kendi default'unu kullanır,
        // user data filter inject (reportId=0 → genel filter'lere düşer). Admin-only.
        [Authorize(Roles = "admin")]
        [HttpGet]
        [Route("Reports/RunJsonV2Preview")]
        public async Task<IActionResult> RunJsonV2Preview(
            string dataSourceKey,
            string procName,
            string? paramSchemaJson = null,
            string? paramsJson = null)
        {
            if (string.IsNullOrWhiteSpace(dataSourceKey) || string.IsNullOrWhiteSpace(procName))
                return Json(new { success = false, error = "Veri kaynağı ve SP adı gerekli." });
            if (!System.Text.RegularExpressions.Regex.IsMatch(procName, @"^[a-zA-Z_][a-zA-Z0-9_\.]*$"))
                return Json(new { success = false, error = "Geçersiz SP adı." });

            var dataSource = await _context.DataSources
                .AsNoTracking()
                .Where(ds => ds.DataSourceKey == dataSourceKey && ds.IsActive)
                .FirstOrDefaultAsync();
            if (dataSource == null)
                return Json(new { success = false, error = "Veri kaynağı bulunamadı veya pasif." });

            var paramFields = ReportParamValidator.ParseSchema(paramSchemaJson);

            var formDict = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            if (!string.IsNullOrWhiteSpace(paramsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson);
                    if (parsed != null)
                        foreach (var kv in parsed)
                            formDict[kv.Key] = new Microsoft.Extensions.Primitives.StringValues(kv.Value ?? "");
                }
                catch { /* default'lara düş */ }
            }
            foreach (var f in paramFields)
            {
                if (formDict.ContainsKey(f.Name)) continue;
                var dv = string.Equals(f.Type, "date", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(f.DefaultValue, "today", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : f.DefaultValue ?? "";
                formDict[f.Name] = new Microsoft.Extensions.Primitives.StringValues(dv);
            }
            var fakeForm = new Microsoft.AspNetCore.Http.FormCollection(formDict);

            var validation = ReportParamValidator.ValidateAndBuild(paramFields, fakeForm);
            if (!validation.Success)
                return Json(new { success = false, error = string.Join(" ", validation.Errors) });

            // reportId yok → 0; UserDataFilterInjector genel (rapor-bağımsız) filter'leri uygular.
            await _filterInjector.InjectAsync(validation.Parameters, CurrentUserId, 0, dataSourceKey);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var resultSets = await _spExecutor.ExecuteMultipleAsync(
                    dataSource.ConnString, procName, validation.Parameters);
                stopwatch.Stop();

                var serialized = resultSets.Select((rs, i) =>
                {
                    var first = rs.FirstOrDefault();
                    var cols = first != null ? first.Keys.ToList() : new List<string>();
                    return new
                    {
                        index = i,
                        name = GuessResultSetTitle(rs, cols, i),
                        columns = cols,
                        rows = rs.Take(50).ToList(),
                        rowCount = rs.Count
                    };
                }).ToList();

                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "report_preview",
                    TargetType = "datasource",
                    TargetKey = dataSourceKey,
                    DataSourceKey = dataSourceKey,
                    ParamsJson = validation.ParamsJson,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    ResultRowCount = resultSets.Sum(rs => rs.Count),
                    IsSuccess = true,
                    Description = $"V2 builder preview: {procName}"
                });

                return Json(new { success = true, error = (string?)null, resultSets = serialized });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "report_preview",
                    TargetType = "datasource",
                    TargetKey = dataSourceKey,
                    DataSourceKey = dataSourceKey,
                    ParamsJson = validation.ParamsJson,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Description = $"V2 builder preview failed: {procName}"
                });
                return Json(new
                {
                    success = false,
                    error = "Rapor çalıştırılamadı. SP, veri kaynağı veya parametreleri kontrol edin.",
                    resultSets = Array.Empty<object>()
                });
            }
        }

        // V2 builder F-9: Tam dashboard preview iframe için DashboardRenderer çıktısı.
        // Draft configJson kaydedilmeden çalıştırılabilir; V1 Run path'inin paritesi (filter inject + SP execute)
        // ama config DB yerine body'den gelir. Admin-only + AntiForgery zorunlu.
        // Response: tam HTML doc (DashboardRenderer.Render full <html><head><body> emit eder).
        [Authorize(Roles = "admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Admin/Reports/PreviewDashboardV2")]
        public async Task<IActionResult> PreviewDashboardV2(
            [FromForm] string dataSourceKey,
            [FromForm] string procName,
            [FromForm] string configJson,
            [FromForm] string? paramsJson = null,
            [FromForm] string? paramSchemaJson = null,
            [FromForm] int? reportId = null)
        {
            if (string.IsNullOrWhiteSpace(dataSourceKey) || string.IsNullOrWhiteSpace(procName))
                return BadRequest("Veri kaynağı ve SP adı gerekli.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(procName, @"^[a-zA-Z_][a-zA-Z0-9_\.]*$"))
                return BadRequest("Geçersiz SP adı.");
            if (string.IsNullOrWhiteSpace(configJson))
                return BadRequest("Pano yapılandırması boş.");

            // 1. Config validate — invalid ise render etme, hata listesi dön
            var configValidation = Services.DashboardConfigValidator.Validate(configJson);
            if (configValidation.HasErrors)
                return BadRequest("Pano yapılandırması geçersiz:\n• " + string.Join("\n• ", configValidation.Errors));

            // 2. DataSource
            var dataSource = await _context.DataSources
                .AsNoTracking()
                .Where(ds => ds.DataSourceKey == dataSourceKey && ds.IsActive)
                .FirstOrDefaultAsync();
            if (dataSource == null)
                return BadRequest("Veri kaynağı bulunamadı veya pasif.");

            // 3. ParamSchema parse — { "fields": [...] } wrapper + legacy format destekli
            var paramFields = ReportParamValidator.ParseSchema(paramSchemaJson);

            // 4. paramsJson + ParamSchema default → fakeForm
            var formDict = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            if (!string.IsNullOrWhiteSpace(paramsJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson);
                    if (parsed != null)
                        foreach (var kv in parsed)
                            formDict[kv.Key] = new Microsoft.Extensions.Primitives.StringValues(kv.Value ?? "");
                }
                catch { /* default'lara düş */ }
            }
            foreach (var f in paramFields)
            {
                if (formDict.ContainsKey(f.Name)) continue;
                var dv = string.Equals(f.Type, "date", StringComparison.OrdinalIgnoreCase) &&
                         string.Equals(f.DefaultValue, "today", StringComparison.OrdinalIgnoreCase)
                    ? DateTime.Today.ToString("yyyy-MM-dd")
                    : f.DefaultValue ?? "";
                formDict[f.Name] = new Microsoft.Extensions.Primitives.StringValues(dv);
            }
            var fakeForm = new Microsoft.AspNetCore.Http.FormCollection(formDict);

            var validation = ReportParamValidator.ValidateAndBuild(paramFields, fakeForm);
            if (!validation.Success)
                return BadRequest(string.Join(" ", validation.Errors));

            await _filterInjector.InjectAsync(validation.Parameters, CurrentUserId, reportId ?? 0, dataSourceKey);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var resultSets = await _spExecutor.ExecuteMultipleAsync(
                    dataSource.ConnString, procName, validation.Parameters);
                stopwatch.Stop();

                DashboardConfig? dashConfig;
                try
                {
                    dashConfig = JsonSerializer.Deserialize<DashboardConfig>(configJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException jx)
                {
                    await _auditLog.LogAsync(new AuditLogEntry
                    {
                        EventType = "dashboard_config_invalid",
                        TargetType = "datasource",
                        TargetKey = dataSourceKey,
                        ReportId = reportId,
                        DataSourceKey = dataSourceKey,
                        Description = $"PreviewDashboardV2 configOverride deserialize failed: {jx.Message}",
                        IsSuccess = false
                    });
                    return BadRequest("Pano yapılandırması parse edilemedi.");
                }
                if (dashConfig == null) return BadRequest("Pano yapılandırması boş.");

                var html = ReportPanel.Services.DashboardRenderer.Render(dashConfig, resultSets);

                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "dashboard_preview",
                    TargetType = "datasource",
                    TargetKey = dataSourceKey,
                    ReportId = reportId,
                    DataSourceKey = dataSourceKey,
                    ParamsJson = validation.ParamsJson,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    ResultRowCount = resultSets.Sum(rs => rs.Count),
                    IsSuccess = true,
                    Description = $"V2 builder full preview: {procName}"
                });

                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "dashboard_preview",
                    TargetType = "datasource",
                    TargetKey = dataSourceKey,
                    ReportId = reportId,
                    DataSourceKey = dataSourceKey,
                    ParamsJson = validation.ParamsJson,
                    DurationMs = (int)stopwatch.ElapsedMilliseconds,
                    IsSuccess = false,
                    ErrorMessage = ex.Message,
                    Description = $"V2 builder full preview failed: {procName}"
                });
                // M-02: generic mesaj, detay audit log'a
                return StatusCode(500, "Önizleme oluşturulamadı. SP veya veri kaynağı kontrol edin, audit log'a bakın.");
            }
        }
    }
}
