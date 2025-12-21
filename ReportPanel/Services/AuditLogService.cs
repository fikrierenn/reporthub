using System.Text.Json;
using Microsoft.AspNetCore.Http;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    public class AuditLogService
    {
        private readonly ReportPanelContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditLogService(ReportPanelContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAsync(AuditLogEntry entry)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var username = entry.Username;
            if (string.IsNullOrWhiteSpace(username))
            {
                username = httpContext?.User?.Identity?.Name ?? "user";
            }

            var log = new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Username = username,
                EventType = entry.EventType ?? "audit",
                TargetType = entry.TargetType,
                TargetKey = entry.TargetKey,
                Description = entry.Description,
                OldValuesJson = entry.OldValuesJson,
                NewValuesJson = entry.NewValuesJson,
                IsSuccess = entry.IsSuccess,
                ErrorMessage = entry.ErrorMessage,
                CreatedAt = DateTime.Now,
                ReportId = entry.ReportId,
                DataSourceKey = entry.DataSourceKey,
                ParamsJson = entry.ParamsJson,
                DurationMs = entry.DurationMs,
                ResultRowCount = entry.ResultRowCount,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString()
            };

            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        public static string ToJson(object? value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false
            });
        }
    }

    public sealed class AuditLogEntry
    {
        public string? Username { get; set; }
        public int? ReportId { get; set; }
        public string? DataSourceKey { get; set; }
        public string? ParamsJson { get; set; }
        public int? DurationMs { get; set; }
        public int? ResultRowCount { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string? EventType { get; set; }
        public string? TargetType { get; set; }
        public string? TargetKey { get; set; }
        public string? Description { get; set; }
        public string? OldValuesJson { get; set; }
        public string? NewValuesJson { get; set; }
    }
}
