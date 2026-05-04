using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit). User-facing rapor çalıştırma:
    // Run GET (otomatik veya formlu), Run POST (parametre validasyonu + SP exec +
    // dashboard render), Export (Excel ihrac).
    public partial class ReportsController
    {
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

            var validation = ReportParamValidator.ValidateAndBuild(context.ParamFields, form);
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
            try
            {
                await _filterInjector.InjectAsync(
                    validation.Parameters,
                    CurrentUserId,
                    context.SelectedReport.ReportId,
                    context.SelectedReport.DataSourceKey);
            }
            catch (UserDataFilterDeniedException ex)
            {
                // Plan 07 Faz 4: deny-by-default — atlanan/eksik veri filtresi → 403.
                await LogDataFilterDenyAsync(ex, context.SelectedReport.ReportId, context.SelectedReport.DataSourceKey);
                Response.StatusCode = 403;
                model.RunError = "Veri filtreniz atanmamis. Lütfen yöneticinize başvurun.";
                return View("Run", model);
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export(int reportId)
        {
            var context = await BuildReportsContext(reportId, null, null);
            if (context.SelectedReport == null)
            {
                return BadRequest("Report not found or access denied.");
            }

            var paramFields = ReportParamValidator.ParseSchema(context.SelectedReport.ParamSchemaJson);
            var validation = ReportParamValidator.ValidateAndBuild(paramFields, Request.Form);
            if (!validation.Success)
            {
                return BadRequest(string.Join(" ", validation.Errors));
            }

            if (context.SelectedReport.DataSource == null || !context.SelectedReport.DataSource.IsActive)
            {
                return BadRequest("Data source not found or inactive.");
            }

            // Plan 07 Faz 4: Export'a UserDataFilterInjector eklendi (mevcut multi-tenant guvenlik gap'i).
            try
            {
                await _filterInjector.InjectAsync(
                    validation.Parameters,
                    CurrentUserId,
                    context.SelectedReport.ReportId,
                    context.SelectedReport.DataSourceKey);
            }
            catch (UserDataFilterDeniedException ex)
            {
                await LogDataFilterDenyAsync(ex, context.SelectedReport.ReportId, context.SelectedReport.DataSourceKey);
                return StatusCode(403, "Veri filtreniz atanmamis. Lütfen yöneticinize başvurun.");
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
    }
}
