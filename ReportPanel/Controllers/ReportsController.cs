using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.Services;
using ReportPanel.ViewModels;

namespace ReportPanel.Controllers
{
    // Partial split (csharp-conventions hard-limit 500). Ana dosya: ctor + DI fields +
    // user identity helpers + Index + Favorite/Unfavorite + private helpers
    // (BuildReportsContext, GetCurrentUserRoleIds, LogRun, GuessResultSetTitle, ResolveViewMode).
    // V2Preview.cs: RunJsonV2 + RunJsonV2Preview + PreviewDashboardV2 (admin builder runtime).
    // Run.cs: Run GET/POST + Export (user-facing rapor çalıştırma + Excel ihrac).
    [Authorize]
    public partial class ReportsController : Controller
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

        // Result set'e generic bir default başlık üret. Sektör-özel tahmin yok —
        // anlamlı isimlendirme admin tarafından Drawer Veri tab'ında yapılır ve
        // DashboardConfigJson.resultContract'a kaydedilir. Bu helper sadece
        // hiç override yokken görünecek "Veri Seti 1" tarzı default sağlar.
        private static string GuessResultSetTitle(
            List<Dictionary<string, object>> rows,
            List<string> columns,
            int index)
        {
            return $"Veri Seti {index + 1}";
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
                    ? ReportParamValidator.ParseSchema(selectedReport.ParamSchemaJson)
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
