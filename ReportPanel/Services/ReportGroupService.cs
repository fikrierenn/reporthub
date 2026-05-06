using Microsoft.EntityFrameworkCore;
using ReportPanel.Models;

namespace ReportPanel.Services
{
    /// <summary>
    /// M-01: ReportGroup CRUD (create/update/delete). G-04 audit log calls.
    /// Plan 07 son rename: CategoryManagementService → ReportGroupService.
    /// </summary>
    public class ReportGroupService
    {
        private readonly ReportPanelContext _context;
        private readonly AuditLogService _auditLog;

        public ReportGroupService(ReportPanelContext context, AuditLogService auditLog)
        {
            _context = context;
            _auditLog = auditLog;
        }

        public async Task<AdminOperationResult> CreateAsync(string? name, string? description, bool isActive)
        {
            var trimmedName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return AdminOperationResult.Fail("Grup adi zorunludur.");

            var exists = await _context.ReportGroups
                .AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());
            if (exists)
                return AdminOperationResult.Fail("Ayni isimde grup zaten var.");

            var entity = new ReportGroup
            {
                Name = trimmedName,
                Description = description ?? "",
                IsActive = isActive
            };
            _context.ReportGroups.Add(entity);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "group_create",
                TargetType = "group",
                TargetKey = entity.GroupId.ToString(),
                Description = "Group created",
                NewValuesJson = AuditLogService.ToJson(new { entity.GroupId, entity.Name, entity.Description, entity.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Grup eklendi.");
        }

        public async Task<AdminOperationResult> UpdateAsync(int groupId, string? name, string? description, bool isActive)
        {
            var group = await _context.ReportGroups.FindAsync(groupId);
            if (group == null) return AdminOperationResult.Fail("Grup bulunamadi.");

            var trimmedName = (name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                return AdminOperationResult.Fail("Grup adi zorunludur.");

            var duplicate = await _context.ReportGroups
                .AnyAsync(c => c.GroupId != group.GroupId && c.Name.ToLower() == trimmedName.ToLower());
            if (duplicate)
                return AdminOperationResult.Fail("Ayni isimde grup zaten var.");

            var oldSnap = new { group.GroupId, group.Name, group.Description, group.IsActive };

            group.Name = trimmedName;
            group.Description = description ?? "";
            group.IsActive = isActive;
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "group_update",
                TargetType = "group",
                TargetKey = group.GroupId.ToString(),
                Description = "Group updated",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                NewValuesJson = AuditLogService.ToJson(new { group.GroupId, group.Name, group.Description, group.IsActive }),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Grup guncellendi.");
        }

        public async Task<AdminOperationResult> DeleteAsync(int groupId)
        {
            var group = await _context.ReportGroups.FindAsync(groupId);
            if (group == null) return AdminOperationResult.Fail("Grup bulunamadi.");

            var oldSnap = new { group.GroupId, group.Name, group.Description, group.IsActive };

            _context.ReportGroups.Remove(group);
            await _context.SaveChangesAsync();

            await _auditLog.LogAsync(new AuditLogEntry
            {
                EventType = "group_delete",
                TargetType = "group",
                TargetKey = group.GroupId.ToString(),
                Description = "Group deleted",
                OldValuesJson = AuditLogService.ToJson(oldSnap),
                IsSuccess = true
            });

            return AdminOperationResult.Ok("Grup silindi.");
        }
    }
}
