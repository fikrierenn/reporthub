using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;
using ReportPanel.ViewModels;
using System.Security.Claims;

namespace ReportPanel.Controllers
{
    [Authorize(Roles = "admin")]
    public class LogsController : Controller
    {
        private readonly ReportPanelContext _context;

        public LogsController(ReportPanelContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string logSearch = "",
            string logStart = "",
            string logEnd = "",
            string eventType = "",
            string logUser = "",
            string targetType = "",
            string success = "",
            int page = 1)
        {
            var userName = User.Identity?.Name ?? "user";
            var isAdmin = User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .Any(role => role.Equals("admin", StringComparison.OrdinalIgnoreCase));

            var model = new LogsViewModel
            {
                LogSearch = logSearch ?? "",
                LogStart = logStart ?? "",
                LogEnd = logEnd ?? "",
                SelectedEventType = eventType ?? "",
                SelectedUser = logUser ?? "",
                SelectedTargetType = targetType ?? "",
                SelectedSuccess = success ?? "",
                IsAdmin = isAdmin,
                Page = page < 1 ? 1 : page
            };

            var logsQuery = _context.AuditLogs.AsQueryable();

            if (!isAdmin)
            {
                logsQuery = logsQuery.Where(l => l.Username == userName);
            }

            if (!string.IsNullOrWhiteSpace(logSearch))
            {
                var pattern = $"%{logSearch.Trim()}%";
                logsQuery = logsQuery.Where(l =>
                    EF.Functions.Like(l.Username ?? "", pattern) ||
                    EF.Functions.Like(l.EventType ?? "", pattern) ||
                    EF.Functions.Like(l.TargetType ?? "", pattern) ||
                    EF.Functions.Like(l.TargetKey ?? "", pattern) ||
                    EF.Functions.Like(l.Description ?? "", pattern));
            }

            if (DateTime.TryParse(logStart, out var startDate))
            {
                var start = startDate.Date;
                logsQuery = logsQuery.Where(l => l.CreatedAt >= start);
            }

            if (DateTime.TryParse(logEnd, out var endDate))
            {
                var end = endDate.Date.AddDays(1);
                logsQuery = logsQuery.Where(l => l.CreatedAt < end);
            }

            if (!string.IsNullOrWhiteSpace(eventType))
            {
                logsQuery = logsQuery.Where(l => l.EventType == eventType);
            }

            if (!string.IsNullOrWhiteSpace(logUser))
            {
                logsQuery = logsQuery.Where(l => l.Username == logUser);
            }

            if (!string.IsNullOrWhiteSpace(targetType))
            {
                logsQuery = logsQuery.Where(l => l.TargetType == targetType);
            }

            if (!string.IsNullOrWhiteSpace(success))
            {
                var isSuccess = string.Equals(success, "true", StringComparison.OrdinalIgnoreCase);
                logsQuery = logsQuery.Where(l => l.IsSuccess == isSuccess);
            }

            model.EventTypes = await _context.AuditLogs
                .Where(l => !string.IsNullOrWhiteSpace(l.EventType))
                .Select(l => l.EventType!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            model.Usernames = await _context.AuditLogs
                .Where(l => !string.IsNullOrWhiteSpace(l.Username))
                .Select(l => l.Username)
                .Distinct()
                .OrderBy(u => u)
                .ToListAsync();

            model.TargetTypes = await _context.AuditLogs
                .Where(l => !string.IsNullOrWhiteSpace(l.TargetType))
                .Select(l => l.TargetType!)
                .Distinct()
                .OrderBy(t => t)
                .ToListAsync();

            model.TotalCount = await logsQuery.CountAsync();
            model.TotalPages = (int)Math.Ceiling(model.TotalCount / (double)model.PageSize);
            if (model.Page > model.TotalPages && model.TotalPages > 0)
            {
                model.Page = model.TotalPages;
            }

            model.Logs = await logsQuery
                .OrderByDescending(l => l.CreatedAt)
                .Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize)
                .ToListAsync();

            return View(model);
        }
    }
}
