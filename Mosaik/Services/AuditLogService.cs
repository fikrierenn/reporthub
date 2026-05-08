using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Mosaik.Core.Logging;
using Mosaik.Models;

namespace Mosaik.Services
{
    public class AuditLogService : IAuditLog
    {
        private readonly MosaikContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<AuditLogService> _logger;

        public AuditLogService(
            MosaikContext context,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AuditLogService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
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
                CreatedAt = DateTime.UtcNow,
                ReportId = entry.ReportId,
                DataSourceKey = entry.DataSourceKey,
                ParamsJson = entry.ParamsJson,
                DurationMs = entry.DurationMs,
                ResultRowCount = entry.ResultRowCount,
                IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                UserAgent = httpContext?.Request.Headers.UserAgent.ToString()
            };

            try
            {
                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Audit log yazımı iş aksiyonunu KIRMAZ — Single Point of Failure önlemi.
                // Detay log'a, çağıran metoda exception bubble etmez.
                _logger.LogError(ex,
                    "AuditLogService.LogAsync: kayıt yazılamadı. EventType={EventType} TargetType={TargetType} TargetKey={TargetKey}",
                    log.EventType, log.TargetType, log.TargetKey);
            }
        }

        // Plan 17 v2 — IAuditLog cross-modül erişim. Modüller bunu çağırır.
        public Task LogAsync(
            string eventType,
            string? targetType = null,
            string? targetKey = null,
            string? description = null,
            string? oldValuesJson = null,
            string? newValuesJson = null,
            bool isSuccess = true)
        {
            return LogAsync(new AuditLogEntry
            {
                EventType = eventType,
                TargetType = targetType,
                TargetKey = targetKey,
                Description = description,
                OldValuesJson = oldValuesJson,
                NewValuesJson = newValuesJson,
                IsSuccess = isSuccess
            });
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
