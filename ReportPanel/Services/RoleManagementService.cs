using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    /// <summary>
    /// M-01: Role CRUD + ReportCatalog.AllowedRoles CSV propagation.
    /// CSV propagation geçici — ADR-004 adayı (madde 26): AllowedRoles CSV deprecate
    /// edilince bu helper'lar da silinecek.
    /// </summary>
    public class RoleManagementService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public RoleManagementService(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<AdminOperationResult> CreateAsync(string? name, string? description, bool isActive)
        {
            var trimmedName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return AdminOperationResult.Fail("Rol adi zorunludur.");

            var exists = await _context.Roles.AnyAsync(r => r.Name.ToLower() == trimmedName.ToLower());
            if (exists)
                return AdminOperationResult.Fail("Ayni isimde rol zaten var.");

            var entity = new Role
            {
                Name = trimmedName,
                Description = description ?? "",
                IsActive = isActive
            };
            _context.Roles.Add(entity);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "role_create",
                TargetType = "role",
                TargetKey = entity.RoleId.ToString(),
                Description = "Role created",
                NewValuesJson = AuditLogService.ToJson(new { entity.RoleId, entity.Name, entity.Description, entity.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Rol eklendi.");
        }

        public async Task<AdminOperationResult> UpdateAsync(int roleId, string? name, string? description, bool isActive)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return AdminOperationResult.Fail("Rol bulunamadi.");

            var trimmedName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return AdminOperationResult.Fail("Rol adi zorunludur.");

            var duplicate = await _context.Roles
                .AnyAsync(r => r.RoleId != role.RoleId && r.Name.ToLower() == trimmedName.ToLower());
            if (duplicate)
                return AdminOperationResult.Fail("Ayni isimde rol zaten var.");

            var oldName = role.Name;
            var oldSnap = new { role.RoleId, Name = oldName, role.Description, role.IsActive };

            role.Name = trimmedName;
            role.Description = description ?? "";
            role.IsActive = isActive;
            await _context.SaveChangesAsync();

            if (!string.Equals(oldName, trimmedName, StringComparison.OrdinalIgnoreCase))
            {
                await PropagateRenameToReportAllowedRolesAsync(oldName, trimmedName);
            }

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "role_update",
                TargetType = "role",
                TargetKey = role.RoleId.ToString(),
                Description = "Role updated",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                NewValuesJson = AuditLogService.ToJson(new { role.RoleId, role.Name, role.Description, role.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Rol guncellendi.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int roleId)
        {
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null) return AdminOperationResult.Fail("Rol bulunamadi.");

            var oldSnap = new { role.RoleId, role.Name, role.Description, role.IsActive };

            await RemoveFromReportAllowedRolesAsync(role.Name);
            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "role_delete",
                TargetType = "role",
                TargetKey = role.RoleId.ToString(),
                Description = "Role deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Rol silindi.");
        }

        // ---- ReportCatalog.AllowedRoles CSV propagation (geçici; madde 26 deprecate edecek) ----

        public async Task PropagateRenameToReportAllowedRolesAsync(string oldName, string newName)
        {
            if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return;
            var reports = await _context.ReportCatalog.ToListAsync();
            foreach (var report in reports)
            {
                var updated = ReplaceCsvValue(report.AllowedRoles, oldName, newName);
                if (!string.Equals(updated, report.AllowedRoles, StringComparison.Ordinal))
                    report.AllowedRoles = updated;
            }
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromReportAllowedRolesAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return;
            var reports = await _context.ReportCatalog.ToListAsync();
            foreach (var report in reports)
            {
                var updated = RemoveCsvValue(report.AllowedRoles, roleName);
                if (!string.Equals(updated, report.AllowedRoles, StringComparison.Ordinal))
                    report.AllowedRoles = updated;
            }
            await _context.SaveChangesAsync();
        }

        private static string ReplaceCsvValue(string csv, string oldValue, string newValue)
        {
            if (string.IsNullOrWhiteSpace(csv)) return "";
            var values = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(v => string.Equals(v, oldValue, StringComparison.OrdinalIgnoreCase) ? newValue : v)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return values.Count == 0 ? "" : string.Join(",", values);
        }

        private static string RemoveCsvValue(string csv, string valueToRemove)
        {
            if (string.IsNullOrWhiteSpace(csv)) return "";
            var values = csv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(v => !string.Equals(v, valueToRemove, StringComparison.OrdinalIgnoreCase))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            return values.Count == 0 ? "" : string.Join(",", values);
        }
    }
}
