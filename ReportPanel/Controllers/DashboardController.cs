using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.ViewModels;
using System.Security.Claims;

namespace ReportPanel.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ReportPanelContext _context;

        public DashboardController(ReportPanelContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userName = User.Identity?.Name ?? "user";
            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Distinct()
                .ToArray();
            var fullName = User.Claims.FirstOrDefault(c => c.Type == "full_name")?.Value ?? "";

            var model = new DashboardViewModel
            {
                User = userName,
                FullName = fullName,
                UserRoles = roles
            };

            var userRolesCsv = string.Join(",", roles);

            // ---- Erişilebilir raporlar ----
            var allReports = await _context.ReportCatalog
                .AsNoTracking()
                .Include(r => r.DataSource)
                .Where(r => r.IsActive && r.DataSource != null && r.DataSource.IsActive)
                .OrderBy(r => r.Title)
                .ToListAsync();

            var accessibleReports = allReports
                .Where(r => AllowedForUser(r.AllowedRoles, userRolesCsv))
                .ToList();

            model.ReportCount = accessibleReports.Count;

            // ---- Audit log tabanlı KPI'lar ----
            var nowUtc = DateTime.UtcNow;
            var startOfDay = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, DateTimeKind.Utc);
            var startOfMonth = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var oneHourAgo = nowUtc.AddHours(-1);

            // Bugün çalıştırılan (toplam, tüm kullanıcılar)
            model.TodayRunCount = await _context.AuditLogs
                .AsNoTracking()
                .CountAsync(l => l.EventType == "report_run" && l.CreatedAt >= startOfDay);

            // Bu ay (sadece bu kullanıcı için)
            model.MonthlyRunCount = await _context.AuditLogs
                .AsNoTracking()
                .CountAsync(l => l.Username == userName && l.EventType == "report_run" && l.CreatedAt >= startOfMonth);

            // Aktif kullanıcı (son 1 saat distinct username)
            model.ActiveUserCount = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt >= oneHourAgo && !string.IsNullOrEmpty(l.Username))
                .Select(l => l.Username)
                .Distinct()
                .CountAsync();

            // ---- En çok çalıştırılan top-5 (son 30 gün) ----
            var thirtyDaysAgo = nowUtc.AddDays(-30);
            var topRunRaw = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.EventType == "report_run" && l.CreatedAt >= thirtyDaysAgo && l.TargetKey != null)
                .GroupBy(l => l.TargetKey)
                .Select(g => new { TargetKey = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToListAsync();

            // TargetKey = ReportId string. Title için lookup.
            var reportIds = topRunRaw
                .Select(x => int.TryParse(x.TargetKey, out var id) ? id : 0)
                .Where(id => id > 0)
                .ToList();
            var titleLookup = await _context.ReportCatalog
                .AsNoTracking()
                .Where(r => reportIds.Contains(r.ReportId))
                .ToDictionaryAsync(r => r.ReportId, r => r.Title);

            model.TopRunReports = topRunRaw
                .Select(x =>
                {
                    var id = int.TryParse(x.TargetKey, out var v) ? v : 0;
                    var title = titleLookup.TryGetValue(id, out var t) ? t : (x.TargetKey ?? "?");
                    return new TopRunReportItem { Title = title, Count = x.Count };
                })
                .ToList();

            // ---- Favoriler (kullanıcının) ----
            var currentUserId = await _context.Users
                .AsNoTracking()
                .Where(u => u.Username == userName)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync();

            var favoriteIds = currentUserId.HasValue
                ? await _context.ReportFavorites
                    .AsNoTracking()
                    .Where(f => f.UserId == currentUserId.Value)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => f.ReportId)
                    .Take(6)
                    .ToListAsync()
                : new List<int>();

            if (favoriteIds.Count > 0)
            {
                var favReports = await _context.ReportCatalog
                    .AsNoTracking()
                    .Include(r => r.DataSource)
                    .Where(r => favoriteIds.Contains(r.ReportId) && r.IsActive)
                    .ToListAsync();
                // Favori sırasını koru
                model.FavoriteReports = favoriteIds
                    .Select(id => favReports.FirstOrDefault(r => r.ReportId == id))
                    .Where(r => r != null && AllowedForUser(r!.AllowedRoles, userRolesCsv))
                    .Cast<ReportCatalog>()
                    .ToList();
            }

            // Eğer favori yoksa veya az ise, en son çalıştırılan raporlardan tamamla
            if (model.FavoriteReports.Count < 6)
            {
                var need = 6 - model.FavoriteReports.Count;
                var recent = accessibleReports
                    .Where(r => !model.FavoriteReports.Any(f => f.ReportId == r.ReportId))
                    .Take(need)
                    .ToList();
                model.FavoriteReports.AddRange(recent);
            }

            // ---- Son aktivite (tüm kullanıcılar, son 7) ----
            model.RecentLogs = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.EventType == "report_run" || l.EventType == "report_create" || l.EventType == "report_update" || l.EventType == "export")
                .OrderByDescending(l => l.CreatedAt)
                .Take(7)
                .ToListAsync();

            model.LastRunAt = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.Username == userName && l.EventType == "report_run")
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => (DateTime?)l.CreatedAt)
                .FirstOrDefaultAsync();

            return View(model);
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
    }
}
