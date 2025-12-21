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

            var model = new DashboardViewModel
            {
                User = userName,
                UserRoles = roles
            };

            var userRolesCsv = string.Join(",", roles);
            var allReports = await _context.ReportCatalog
                .Include(r => r.DataSource)
                .Where(r => r.IsActive && r.DataSource != null && r.DataSource.IsActive)
                .OrderBy(r => r.Title)
                .ToListAsync();

            var accessibleReports = allReports
                .Where(r => AllowedForUser(r.AllowedRoles, userRolesCsv))
                .ToList();

            model.ReportCount = accessibleReports.Count;
            model.RecentReports = accessibleReports.Take(5).ToList();

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthlyRunCount = await _context.AuditLogs
                .CountAsync(l => l.Username == userName &&
                                 l.EventType == "report_run" &&
                                 l.CreatedAt >= startOfMonth);
            model.MonthlyRunCount = monthlyRunCount;

            var userLogs = await _context.AuditLogs
                .Where(l => l.Username == userName && l.EventType == "report_run")
                .OrderByDescending(l => l.CreatedAt)
                .Take(5)
                .ToListAsync();

            model.RecentLogs = userLogs;
            model.LastRunAt = userLogs.FirstOrDefault()?.CreatedAt;

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
