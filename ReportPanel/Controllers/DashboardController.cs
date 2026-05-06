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

            model.HasUserFavorites = favoriteIds.Count > 0;
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

            // ---- Saatlik trend (son 24 saat, 24 entry — boş saatler 0) ----
            var startOfTrend = nowUtc.AddHours(-23);
            // Saat başına yuvarla (mevcut saatin başlangıcı)
            startOfTrend = new DateTime(startOfTrend.Year, startOfTrend.Month, startOfTrend.Day, startOfTrend.Hour, 0, 0, DateTimeKind.Utc);

            var hourlyRaw = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.EventType == "report_run" && l.CreatedAt >= startOfTrend)
                .GroupBy(l => new { l.CreatedAt.Year, l.CreatedAt.Month, l.CreatedAt.Day, l.CreatedAt.Hour })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    g.Key.Day,
                    g.Key.Hour,
                    Count = g.Count()
                })
                .ToListAsync();

            var hourlyMap = hourlyRaw.ToDictionary(
                h => new DateTime(h.Year, h.Month, h.Day, h.Hour, 0, 0, DateTimeKind.Utc),
                h => h.Count);

            // 24 saatlik dolu liste (boş saatler 0)
            for (int i = 0; i < 24; i++)
            {
                var slot = startOfTrend.AddHours(i);
                model.HourlyTrend.Add(new HourlyTrendItem
                {
                    Hour = slot,
                    Count = hourlyMap.TryGetValue(slot, out var c) ? c : 0
                });
            }

            // ---- DataSource durumu (aktif/toplam + son SP exec response) ----
            var dsList = await _context.DataSources
                .AsNoTracking()
                .Select(d => new { d.Title, d.DataSourceKey, d.IsActive })
                .ToListAsync();
            var activeDsCount = dsList.Count(d => d.IsActive);
            var totalDsCount = dsList.Count;
            var primaryDs = dsList.FirstOrDefault(d => d.IsActive);

            // ---- Sistem durumu ----
            // Kontrol: son 1 saatte audit log error oranı, aktif DataSource oranı
            var oneHourLogs = await _context.AuditLogs
                .AsNoTracking()
                .Where(l => l.CreatedAt >= oneHourAgo && l.EventType == "report_run")
                .Select(l => new { l.IsSuccess })
                .ToListAsync();
            var totalRecent = oneHourLogs.Count;
            var failRecent = oneHourLogs.Count(l => !l.IsSuccess);
            var failRatio = totalRecent > 0 ? (double)failRecent / totalRecent : 0.0;

            if (activeDsCount == 0)
            {
                model.SystemStatus = "veri kaynağı yok";
                model.SystemStatusKind = "err";
            }
            else if (failRatio > 0.20 && totalRecent >= 5)
            {
                model.SystemStatus = $"son saatte %{(int)(failRatio * 100)} hata";
                model.SystemStatusKind = "warn";
            }
            else if (activeDsCount < totalDsCount)
            {
                model.SystemStatus = $"{activeDsCount}/{totalDsCount} kaynak aktif";
                model.SystemStatusKind = "warn";
            }
            else
            {
                model.SystemStatus = "tüm sistemler çalışıyor";
                model.SystemStatusKind = "ok";
            }

            // DataSource pill — primary kaynak adı + son SP duration tahmini (audit log'da DurationMs varsa)
            if (primaryDs != null)
            {
                var avgDurationMs = await _context.AuditLogs
                    .AsNoTracking()
                    .Where(l => l.EventType == "report_run" && l.CreatedAt >= oneHourAgo && l.DurationMs.HasValue)
                    .Select(l => l.DurationMs!.Value)
                    .ToListAsync();
                if (avgDurationMs.Count > 0)
                {
                    var avg = (int)avgDurationMs.Average();
                    var latency = avg > 1000 ? $"{avg / 1000.0:F1}s" : $"{avg}ms";
                    model.DataSourceStatus = $"{primaryDs.Title} · {latency}";
                }
                else
                {
                    model.DataSourceStatus = $"{primaryDs.Title} · {activeDsCount} kaynak";
                }
            }
            else
            {
                model.DataSourceStatus = "kaynak yok";
            }

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
