using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    /// <summary>
    /// M-01: DataSource CRUD + connection test. AdminController.HandlePostAction
    /// ile dedicated CreateDataSource / EditDataSource action'lari bu servisi kullanir.
    /// </summary>
    public class DataSourceManagementService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public DataSourceManagementService(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<AdminOperationResult> CreateAsync(string? key, string? title, string? connString, bool isActive)
        {
            var normalizedKey = (key ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(normalizedKey))
                return AdminOperationResult.Fail("Veri kaynagi anahtari zorunludur.");
            if (string.IsNullOrWhiteSpace(title))
                return AdminOperationResult.Fail("Baslik zorunludur.");

            var exists = await _context.DataSources.AnyAsync(d => d.DataSourceKey == normalizedKey);
            if (exists)
                return AdminOperationResult.Fail("Ayni anahtar ile veri kaynagi zaten var.");

            var entity = new DataSource
            {
                DataSourceKey = normalizedKey,
                Title = title.Trim(),
                ConnString = connString ?? "",
                IsActive = isActive
            };
            _context.DataSources.Add(entity);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "datasource_create",
                TargetType = "datasource",
                TargetKey = entity.DataSourceKey,
                DataSourceKey = entity.DataSourceKey,
                Description = "Data source created",
                NewValuesJson = AuditLogService.ToJson(new { entity.DataSourceKey, entity.Title, entity.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Veri kaynağı eklendi");
        }

        public async Task<AdminOperationResult> UpdateAsync(string? key, string? title, string? connString, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(key)) return AdminOperationResult.Fail("Anahtar gerekli.");
            var ds = await _context.DataSources.FindAsync(key);
            if (ds == null) return AdminOperationResult.Fail("Veri kaynagi bulunamadi.");

            if (string.IsNullOrWhiteSpace(title))
                return AdminOperationResult.Fail("Baslik zorunludur.");

            var oldSnap = new { ds.DataSourceKey, ds.Title, ds.IsActive };

            ds.Title = title.Trim();
            ds.ConnString = connString ?? ds.ConnString;
            ds.IsActive = isActive;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "datasource_update",
                TargetType = "datasource",
                TargetKey = ds.DataSourceKey,
                DataSourceKey = ds.DataSourceKey,
                Description = "Data source updated",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                NewValuesJson = AuditLogService.ToJson(new { ds.DataSourceKey, ds.Title, ds.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Veri kaynağı güncellendi");
        }

        public async Task<AdminOperationResult> DeleteAsync(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return AdminOperationResult.Fail("Anahtar gerekli.");
            var ds = await _context.DataSources.FindAsync(key);
            if (ds == null) return AdminOperationResult.Fail("Veri kaynagi bulunamadi.");

            var oldSnap = new { ds.DataSourceKey, ds.Title, ds.ConnString, ds.IsActive };
            _context.DataSources.Remove(ds);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "datasource_delete",
                TargetType = "datasource",
                TargetKey = ds.DataSourceKey,
                DataSourceKey = ds.DataSourceKey,
                Description = "Data source deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Veri kaynağı silindi");
        }

        /// <summary>
        /// Connection testi: SELECT 1 + audit log success/fail. ex.Message
        /// audit'e yazilir, user'a generic mesaj doner (M-02).
        /// </summary>
        public async Task<AdminOperationResult> TestConnectionAsync(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return AdminOperationResult.Fail("Anahtar gerekli.");
            var ds = await _context.DataSources.FindAsync(key);
            if (ds == null) return AdminOperationResult.Fail("Veri kaynagi bulunamadi.");

            try
            {
                await using var connection = new SqlConnection(ds.ConnString);
                await connection.OpenAsync();
                await using var command = new SqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync();

                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "datasource_test",
                    TargetType = "datasource",
                    TargetKey = ds.DataSourceKey,
                    DataSourceKey = ds.DataSourceKey,
                    Description = "Data source test OK",
                    IsSuccess = true
                });
                return AdminOperationResult.Ok("Bağlantı testi başarılı");
            }
            catch (Exception ex)
            {
                await _auditLog.LogAsync(new AuditLogEntry
                {
                    EventType = "datasource_test",
                    TargetType = "datasource",
                    TargetKey = ds.DataSourceKey,
                    DataSourceKey = ds.DataSourceKey,
                    Description = "Data source test failed",
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                });
                return AdminOperationResult.Fail("Veri kaynağına bağlanılamadı. Bağlantı ayarlarını kontrol edin.");
            }
        }
    }
}
